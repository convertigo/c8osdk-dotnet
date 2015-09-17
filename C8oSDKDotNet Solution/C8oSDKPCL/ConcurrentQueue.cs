using Convertigo.SDK.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Convertigo.SDK.Utils
{
    /// <summary>
    /// Represents a thread-safe first in-first out (FIFO) collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentQueue<T>
    {
        /// <summary>
        /// The front of the queue.
        /// </summary>
        private QueueElement<T> front;
        /// <summary>
        /// The rear of the queue.
        /// </summary>
        private QueueElement<T> rear;
        /// <summary>
        /// Allows to lock on the front or the rear enven if the queue is empty.
        /// </summary>
        // private QueueElement<T> nullElement;

        private Boolean[] locker;

        /// <summary>
        /// The number of elements in the queue.
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                QueueElement<T> item = this.front;
                while (item != null)
                {
                    count++;
                    item = item.next;
                }
                return count;
            }
        }

        public Boolean IsEmpty
        {
            get
            {
                return this.Count == 0;
            }
        }

        /// <summary>
        /// Creates a empty queue.
        /// </summary>
        public ConcurrentQueue()
        {
            // this.nullElement = new QueueElement<T>(default(T));
            this.front = null;//this.nullElement;
            this.rear = this.front;
            this.locker = new Boolean[] {};
        }

        /// <summary>
        /// Adds an element.
        /// </summary>
        /// <param name="value"></param>
        public void Enqueue(T value)
        {
            // Ca ne sécurise pas à 100% :
            // t = 0 : t1 et t2 appellent Enqueue() avec v1 et v2, t1 passe et t2 se bloque
            // Queue : null
            // t = 1 : t1 ajoute sa valeur
            // Queue : v1 -> null
            // t = 2 : t3 appelle Enqueue() avec v3, passe, ajoute sa valeur et se libère
            // Queue : v1 -> v3 -> null
            // t = 3 : t1 se libère, t2 passe, ajoute sa valeur et se libère
            // Queue : v1 -> v3 -> v2 -> null

            QueueElement<T> newRear = new QueueElement<T>(value);

            if (this.rear == null)
            {
                lock (this.locker)
                {
                    if (this.rear == null)
                    {
                        this.rear = newRear;
                        this.front = this.rear;
                        return;
                    }
                }
            }

            lock (this.rear)
            {
                this.rear.next = newRear;
                this.rear = newRear;
            }
        }

        /// <summary>
        /// Returns the next element of the queue and removes it.
        /// </summary>
        /// <returns></returns>
        public T Dequeue()
        {
            if (this.front == null)
            {
                throw new ArgumentNullException(C8oExceptionMessage.ToDo());
            }

            lock (this.front)
            {
                T value = this.front.value;
                // The current front becomes the next element od the previous front
                this.front = this.front.next;
                return value;
            }
        }

    }

    class QueueElement<T>
    {
        public T value;
        public QueueElement<T> next;

        public QueueElement(T value)
        {
            this.value = value;
            this.next = null;
        }
    }

}
