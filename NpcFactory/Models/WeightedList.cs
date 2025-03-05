using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcFactory.Models
{
    class WeightedList<T>
    {
        private readonly List<WeightedListItem<T>> _items = [];
        private uint _maxWeight = 0;

        public int Count
        {
            get
            {
                return _items.Count;
            }
        }

        public void Add(T item, uint weight)
        {
            _items.Add(new WeightedListItem<T>(item, weight));
            _items.Sort((x, y) => (int)x.Weight - (int)y.Weight);
            _maxWeight += weight;
        }

        public T GetRandomItem()
        {
            var randomNumber = Program.Random.Next() % (_maxWeight + 1);
            uint weightSum = 0;

            foreach (var item in _items)
            {
                weightSum += item.Weight;

                if (weightSum >= randomNumber)
                {
                    return item.Underlying;
                }
            }

            return _items.Last().Underlying;
        }

        public bool IsEmpty
        {
            get
            {
                return _items.Count == 0;
            }
        }
    }
}
