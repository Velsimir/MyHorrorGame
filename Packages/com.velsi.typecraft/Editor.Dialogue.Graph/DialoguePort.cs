using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Velsi.Typecraft.Dialogue.Editor.Graph
{
    public sealed class DialoguePort : Port
    {
        private DialoguePort(Direction direction, Capacity capacity) : base(Orientation.Horizontal, direction, capacity, typeof(bool))
        {
        }

        public static DialoguePort Create(Direction direction, Capacity capacity, IEdgeConnectorListener listener)
        {
            var port = new DialoguePort(direction, capacity);
            port.m_EdgeConnector = new EdgeConnector<Edge>(listener);
            port.AddManipulator(port.m_EdgeConnector);
            return port;
        }
    }

    public sealed class DialogueEdgeListener : IEdgeConnectorListener
    {
        private readonly Action<Edge, Vector2> _droppedOutside;
        private readonly List<Edge> _create = new List<Edge>();
        private readonly List<GraphElement> _delete = new List<GraphElement>();

        public DialogueEdgeListener(Action<Edge, Vector2> droppedOutside) => _droppedOutside = droppedOutside;

        public void OnDropOutsidePort(Edge edge, Vector2 position) => _droppedOutside?.Invoke(edge, position);

        public void OnDrop(GraphView graphView, Edge edge)
        {
            _create.Clear();
            _create.Add(edge);
            _delete.Clear();
            Collect(edge.input, edge);
            Collect(edge.output, edge);
            if (_delete.Count > 0)
                graphView.DeleteElements(_delete);

            var change = new GraphViewChange { edgesToCreate = _create };
            List<Edge> created = graphView.graphViewChanged != null ? graphView.graphViewChanged(change).edgesToCreate : _create;
            foreach (Edge entry in created)
            {
                graphView.AddElement(entry);
                edge.input.Connect(entry);
                edge.output.Connect(entry);
            }
        }

        private void Collect(Port port, Edge edge)
        {
            if (port.capacity != Port.Capacity.Single)
                return;

            foreach (Edge existing in port.connections)
            {
                if (existing != edge)
                    _delete.Add(existing);
            }
        }
    }
}
