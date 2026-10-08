using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine.Networking;

namespace Velsi.Typecraft.Editor.Localization
{
    public readonly struct GoogleSheetLink
    {
        public GoogleSheetLink(string id, string gid, bool published)
        {
            Id = id;
            Gid = gid;
            Published = published;
        }

        public string Id { get; }
        public string Gid { get; }
        public bool Published { get; }

        public string ExportUrl => Published
            ? $"https://docs.google.com/spreadsheets/d/e/{Id}/pub?output=csv&gid={Gid}"
            : $"https://docs.google.com/spreadsheets/d/{Id}/export?format=csv&gid={Gid}";
    }

    public static class GoogleSheets
    {
        public const string ClosedMessage = "Таблица закрыта. Открой доступ «Все, у кого есть ссылка — читатель» или используй экспорт в xlsx.";

        private static readonly Regex SheetPath = new Regex(@"/spreadsheets/(?:u/\d+/)?d/(e/)?([A-Za-z0-9_-]{10,})", RegexOptions.IgnoreCase);
        private static readonly Regex GidParameter = new Regex(@"[#?&]gid=(\d+)", RegexOptions.IgnoreCase);
        private static readonly Regex BareId = new Regex(@"^[A-Za-z0-9_-]{25,}$");

        public static bool TryParse(string link, out GoogleSheetLink parsed)
        {
            parsed = default;
            string text = link?.Trim() ?? string.Empty;
            if (text.Length == 0)
                return false;

            if (BareId.IsMatch(text))
            {
                parsed = new GoogleSheetLink(text, "0", false);
                return true;
            }

            Match path = SheetPath.Match(text);
            if (!path.Success || text.IndexOf("docs.google.com", StringComparison.OrdinalIgnoreCase) < 0)
                return false;

            Match gid = GidParameter.Match(text);
            parsed = new GoogleSheetLink(path.Groups[2].Value, gid.Success ? gid.Groups[1].Value : "0", path.Groups[1].Success);
            return true;
        }

        public static string Interpret(long status, string contentType, string body)
        {
            if (status == 401 || status == 403)
                throw new TableSyncException(ClosedMessage);
            if (status == 404)
                throw new TableSyncException("Таблица не найдена — проверь ссылку. Если ссылка верная: " + ClosedMessage);
            if (status == 400)
                throw new TableSyncException("Google не отдал лист (код 400) — проверь gid листа в ссылке.");
            if (status != 0 && (status < 200 || status >= 300))
                throw new TableSyncException($"Google ответил кодом {status}.");

            string text = body ?? string.Empty;
            string head = text.TrimStart('﻿', ' ', '\t', '\r', '\n');
            bool html = (contentType != null && contentType.IndexOf("html", StringComparison.OrdinalIgnoreCase) >= 0)
                        || head.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
                        || head.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
            if (html)
                throw new TableSyncException(ClosedMessage);

            return text;
        }
    }

    public sealed class GoogleSheetsDownload
    {
        private const int TimeoutSeconds = 60;

        private readonly UnityWebRequest _request;
        private readonly Action<string> _completed;
        private readonly Action<string> _failed;
        private bool _finished;

        private GoogleSheetsDownload(string url, Action<string> completed, Action<string> failed)
        {
            _completed = completed;
            _failed = failed;
            _request = UnityWebRequest.Get(url);
            _request.timeout = TimeoutSeconds;
            _request.SetRequestHeader("Accept", "text/csv, text/plain;q=0.9, */*;q=0.1");
        }

        public bool IsRunning => !_finished;
        public ulong DownloadedBytes => _finished ? 0 : _request.downloadedBytes;
        public float Progress => _finished ? 1f : _request.downloadProgress;

        public static GoogleSheetsDownload Start(GoogleSheetLink link, Action<string> completed, Action<string> failed) =>
            Start(link.ExportUrl, completed, failed);

        internal static GoogleSheetsDownload Start(string url, Action<string> completed, Action<string> failed)
        {
            var download = new GoogleSheetsDownload(url, completed, failed);
            download._request.SendWebRequest();
            EditorApplication.update += download.Poll;
            return download;
        }

        public void Cancel()
        {
            if (_finished)
                return;

            _request.Abort();
            Finish();
        }

        private void Poll()
        {
            if (_finished || !_request.isDone)
                return;

            string csv = null;
            string error = null;
            try
            {
                if (_request.result == UnityWebRequest.Result.ConnectionError)
                    error = $"Нет связи с Google: {_request.error}";
                else
                    csv = GoogleSheets.Interpret(_request.responseCode, _request.GetResponseHeader("Content-Type"), _request.downloadHandler?.text);
            }
            catch (TableSyncException exception)
            {
                error = exception.Message;
            }

            Finish();
            if (error != null)
                _failed?.Invoke(error);
            else
                _completed?.Invoke(csv);
        }

        private void Finish()
        {
            _finished = true;
            EditorApplication.update -= Poll;
            _request.Dispose();
        }
    }

    public sealed class TableSyncException : Exception
    {
        public TableSyncException(string message) : base(message)
        {
        }
    }
}
