﻿using System;
using System.Collections.Generic;

using System.Text;

namespace TaskQueue.Providers
{
    /// <summary>
    /// Now support only fifo queue - without update, custom selectors
    /// </summary>
    public class MemQueue : ITQueue
    {

        public class EncapsulatedMessageComparer : IComparer<TaskMessage>
        {
            public MessageComparer internalComparer;
            public EncapsulatedMessageComparer(MessageComparer msgComparer)
            {
                internalComparer = msgComparer;
            }

            public int Compare(TaskMessage x, TaskMessage y)
            {
                int r = internalComparer.Compare(x, y);
                if (r == 0)
                    return ((int)x.Holder["__idx"]).CompareTo((int)y.Holder["__idx"]);
                return r;
            }
        }
        public const string queueTypeName = "InMemoryQueue";

        const int maxTuple = 100;
        RepresentedModel m { get; set; }
        EncapsulatedMessageComparer comparer;
        //Queue<Providers.TaskMessage> baseQueue;

        int Counter = 0;
        SortedSet<TaskMessage> MessageQueue;

        public string Name;

        public MemQueue()
        {
            //baseQueue = new Queue<Providers.TaskMessage>();
            //DateTime dt = DateTime.UtcNow;
            //Providers.TaskMessage[] tarrtp = new Providers.TaskMessage[2000000];
            //for (int i = 0; i < tarrtp.Length; i++)
            //{
            //    tarrtp[i] = new TaskMessage("benchMessage")
            //    {
            //        AddedTime = dt
            //    };
            //    dt = dt.AddSeconds(1);
            //}
            //baseQueue = new Queue<Providers.TaskMessage>(tarrtp);

        }
        public MemQueue(RepresentedModel model, QueueConnectionParameters connection)
        {
            this.InitialiseFromModel(model, connection);
            Name = connection.Name;
        }

        public void Push(Providers.TaskMessage item)
        {
            try
            {
                if (item.Holder == null) item.GetHolder();
                if (comparer.internalComparer.Check(item))
                {
                    lock (MessageQueue)
                    {
                        item.Holder["__idx"] = Counter;
                        MessageQueue.Add(item);
                        Counter++;
                    }
                }
                //lock (baseQueue)
                //    baseQueue.Enqueue(item);

            }
            catch (OutOfMemoryException excOverfl)
            {
                throw new QueueOverflowException(excOverfl);
            }
        }

        //public Providers.TaskMessage GetItemFifo()
        //{
        //    if (baseQueue.Count == 0)
        //        return null;
        //    lock (baseQueue)
        //        return baseQueue.Dequeue();
        //}

        public Providers.TaskMessage GetItem()
        {
            //if (baseQueue.Count == 0)
            //    return null;
            //lock (baseQueue)
            //    return baseQueue.Dequeue();
            if (MessageQueue.Count == 0)
                return null;
            lock (MessageQueue)
            {
                TaskMessage result;

                result = MessageQueue.Min;
                TaskMessage msg = new TaskMessage(result.Holder);
                msg.Holder.Add("__original", result);
                return msg;
            }
        }

        public void InitialiseFromModel(RepresentedModel model, QueueConnectionParameters connection)
        {
            this.m = model;
        }

        public string QueueType
        {
            get { return queueTypeName; }
        }


        public string QueueDescription
        {
            get { return "Non persistent in-memory queue"; }
        }

        public void UpdateItem(Providers.TaskMessage item)
        {
            Dictionary<string, object> holder = item.GetHolder();
            object id = holder["__original"];
            if (id == null || !(id is TaskMessage))
                throw new Exception("__original of queue element is missing");
            TaskMessage orig = (TaskMessage)id;
            holder.Remove("__original");
            lock (MessageQueue)
            {
                MessageQueue.Remove(orig);
                this.Push(item);
            }
        }


        public void OptimiseForSelector()
        {
            //throw new NotImplementedException();
        }

        public Providers.TaskMessage[] GetItemTuple()
        {
            //lock (baseQueue)
            //    if (baseQueue.Count > 0)
            //    {
            //        TaskMessage[] tuple;
            //        if (maxTuple < baseQueue.Count)
            //        {
            //            tuple = new TaskMessage[maxTuple];
            //            for (int i = 0; i < maxTuple; i++)
            //            {
            //                tuple[i] = baseQueue.Dequeue();
            //            }
            //        }
            //        else
            //        {
            //            tuple = baseQueue.ToArray();
            //            baseQueue.Clear();
            //        }
            //        return tuple;

            //    }
            //    else
            //    {
            //        return null;
            //    }
            lock (MessageQueue)
                if (MessageQueue.Count > 0)
                {
                    TaskMessage[] tuple = null;
                    if (maxTuple < MessageQueue.Count)
                    {
                        tuple = new TaskMessage[maxTuple];
                        MessageQueue.CopyTo(tuple, 0, tuple.Length);
                    }
                    else
                    {
                        tuple = new TaskMessage[MessageQueue.Count];
                        MessageQueue.CopyTo(tuple);
                    }
                    for (int i = 0; i < tuple.Length; i++)
                    {
                        TaskMessage em = tuple[i];
                        TaskMessage msg = new TaskMessage(em.Holder);
                        msg.Holder.Add("__original", em);
                        tuple[i] = msg;
                    }
                    return tuple;

                }
                else
                {
                    return null;
                }

        }

        public long GetQueueLength()
        {
            //return baseQueue.Count;
            return MessageQueue.Count;
        }

        public void SetSelector(TQItemSelector selector = null)
        {
            if (selector == null)
                selector = TQItemSelector.DefaultFifoSelector;
            comparer = new EncapsulatedMessageComparer(new MessageComparer(selector));
            MessageQueue = new SortedSet<TaskMessage>(comparer);
        }

        public QueueSpecificParameters GetParametersModel()
        {
            return new MemQueueParams();
        }
    }
    public class MemQueueParams : QueueSpecificParameters
    {
        [TaskQueue.FieldDescription("Flush/restore queue to disk", Required = true, DefaultValue = true)]
        public bool Persistant { get; set; }

        public override bool Validate(out string result)
        {
            result = "";
            return true;
        }
        public override string ItemTypeName
        {
            get
            {
                return this.GetType().Name;
            }
            set
            {

            }
        }
    }
}
