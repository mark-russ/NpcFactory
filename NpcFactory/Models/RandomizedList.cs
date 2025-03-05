using DynamicData;
using Mutagen.Bethesda.Fallout4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NpcFactory.Models
{
    class RandomizedList<T>
    {
        private readonly List<RandomizedListItem<T>> _items = [];

        public int Count
        {
            get
            {
                return _items.Count;
            }
        }

        public void Add(T item, uint chance)
        {
            _items.Add(new RandomizedListItem<T>(item, chance));
        }

        public T GetRandomItem()
        {
            return this._items.First((x) => x.Chance >= Program.Random.Next() % 100).Underlying;
        }

        public IEnumerable<T> GetRandomItems()
        {
            return this._items.Where((x) => x.Chance >= Program.Random.Next() % 100).Select(x => x.Underlying);
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
