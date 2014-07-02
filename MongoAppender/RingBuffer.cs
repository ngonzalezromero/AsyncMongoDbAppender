using System;

namespace MongoAppender
{
    public class RingBuffer<T>
    {
        readonly object lockObject = new object();
        readonly T[] buffer;
        readonly int size;
        int readIndex = 0;
        int writeIndex = 0;
        bool bufferFull;

        public event Action<object, EventArgs> BufferOverflow;

        public RingBuffer(int size)
        {
            this.size = size;
            buffer = new T[size];
        }

        public void Enqueue(T item)
        {
            lock (lockObject)
            {
                buffer[writeIndex] = item;
                writeIndex = (++writeIndex) % size;
                if (bufferFull)
                {
                    if (BufferOverflow != null)
                    {
                        BufferOverflow(this, EventArgs.Empty);
                    }
                    readIndex = writeIndex;
                }
                else
                    bufferFull |= writeIndex == readIndex;
            }
        }

        public bool TryDequeue(out T ret)
        {
            if (readIndex == writeIndex && !bufferFull)
            {
                ret = default(T);
                return false;
            }
            lock (lockObject)
            {
                if (readIndex == writeIndex && !bufferFull)
                {
                    ret = default(T);
                    return false;
                }

                ret = buffer[readIndex];
                readIndex = (++readIndex) % size;
                bufferFull = false;
                return true;
            }
        }
    }
}