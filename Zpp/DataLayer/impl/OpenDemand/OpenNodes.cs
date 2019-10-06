using System.Collections.Generic;
using System.Linq;
using Master40.DB.DataModel;
using Zpp.DataLayer.DemandDomain.Wrappers;
using Zpp.Util;

namespace Zpp.DataLayer.impl.OpenDemand
{
    public class OpenNodes<T>
    {
        private readonly Dictionary<M_Article, List<OpenNode<T>>> _openNodes = new Dictionary<M_Article, List<OpenNode<T>>>();

        public void Add(M_Article article, OpenNode<T> openNode)
        {
            if (openNode.GetOpenNode().GetType() != typeof(StockExchangeDemand))
            {
                throw new MrpRunException("An open provider can only be a StockExchangeDemand.");
            }
            InitOpenProvidersDictionary(article);
            _openNodes[article].Add(openNode);
        }

        public bool AnyOpenProvider(M_Article article)
        {
            InitOpenProvidersDictionary(article);
            return _openNodes[article].Any();
        }

        public List<OpenNode<T>> GetOpenProvider(M_Article article)
        {
            if (AnyOpenProvider(article) == false)
            {
                return null;
            }

            return _openNodes[article];
        }

        public void Remove(OpenNode<T> node)
        {
            _openNodes[node.GetArticle()].RemoveAt(0);
        }

        private void InitOpenProvidersDictionary(M_Article article)
        {
            if (_openNodes.ContainsKey(article) == false)
            {
                _openNodes.Add(article, new List<OpenNode<T>>());
            }
        }

        public void Clear()
        {
            _openNodes.Clear();
        }
    }
}