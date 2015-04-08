using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnyTray
{
    class EventedQueue<T>
    {
        private readonly Queue<T> queue = new Queue<T>();
        public event EventHandler Enqueued;

        protected virtual void OnEnqueued()
        {
            if (Enqueued != null)
                Enqueued(this, new EventArgs());
        }

        public virtual void Enqueue(T item)
        {
            queue.Enqueue(item);
            OnEnqueued();
        }
        public int Count
        {
            get
            {
                return queue.Count;
            }
        }
        public virtual T Dequeue()
        {
            T item = queue.Dequeue();
            OnEnqueued();
            return item;
        }
    } 
}
