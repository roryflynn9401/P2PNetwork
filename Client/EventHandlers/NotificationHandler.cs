using P2PProject.Client.Extensions;
using P2PProject.Client.Models;
using P2PProject.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P2PProject.Client.EventHandlers
{
    public class NotificationHandler
    {
        private Node _node;

        public NotificationHandler(Node node) 
        {
            _node = node;
            _node.MessageReceived += _node_MessageRecieved;
            _node.NodeConnected += _node_NodeConnected;
        }

        private static void _node_MessageRecieved(object sender, MessageEventArgs m)
        {
            
        }

        private static void _node_NodeConnected(object sender, ConnectionEventArgs c)
        {
                        
        }

        private static void _node_NodeDisconnect(object sender, NetworkEventArgs c)
        {          
        }
    }
}
