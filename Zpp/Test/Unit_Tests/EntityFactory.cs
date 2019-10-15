using System.Collections.Generic;
using Master40.DB.Data.WrappersForPrimitives;
using Master40.DB.DataModel;
using Zpp.Util.Graph;
using Zpp.Util.Graph.impl;

namespace Zpp.Test.Unit_Tests
{
    public class EntityFactory
    {
        public static INode[] CreateDummyNodes(int count)
        {
            List<INode> list = new List<INode>();
            for (int i = 0; i < count; i++)
            {
                INode dummyNode = new Node(new DummyNode(new Id(i)));
                list.Add(dummyNode);
            }

            return list.ToArray();
        }
    }
}