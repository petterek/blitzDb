using System;
using System.Collections.Concurrent;
using System.Data;
using System.Runtime.Serialization;
using System.Threading;

namespace blitzdb
{

    /// <summary>
    /// Should only be used when tou need a singleton instance, that is accessed by multiple threads. 
    /// </summary>
    public class BufferdDbAbstraction : IDBAbstraction
    {
        private IDbConnection con;

        DbWriterWorker dbWriterWorker;

        public BufferdDbAbstraction(IDbConnection con)
        {
            this.con = con;
            dbWriterWorker = new DbWriterWorker(con);
        }

        public void Dispose()
        {
            dbWriterWorker.Stop();
            con.Dispose();
        }

        /// <summary>
        /// Will buffer the command, and run it from seperate thread.. Exceptions will be swolllowed.. 
        /// </summary>
        /// <param name="dbCommand"></param>
        public void Execute(IDbCommand dbCommand)
        {
            dbWriterWorker.AddCommand(dbCommand);
        }


        /// <summary>
        /// This does not give any meaning in a buffered evironment. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dbCommand"></param>
        /// <returns></returns>
        public T ExecuteScalar<T>(IDbCommand dbCommand)
        {
            throw new NotImplementedException();
        }
    }

    internal class DbWriterWorker
    {
        readonly IDbConnection connection;
        private Thread Worker;
        private bool IsSleeping = false;
        private ConcurrentQueue<IDbCommand> Commands = new ConcurrentQueue<IDbCommand>();
        private bool stop = false;



        public DbWriterWorker(IDbConnection connection)
        {
            this.connection = connection;
            connection.Open();
            Worker = new Thread(Loop);
            Worker.Start();
        }

        public void Stop()
        {
            Stop(false);
        }

        public void Stop(bool failure)
        {
            stop = true;
                        

            if (IsSleeping && !failure)
            {
                Worker.Interrupt();
            }

            for (var x = 0; x < 60 ; x++)
            {
                if((Commands.Count == 0 || failure) && !isExecutingQuery  )
                {
                    break;
                }
                Thread.Sleep(1000);
            }
            
            
            connection.Close();

            if (Commands.Count > 0) throw new UnableToWriteAllCommandsToDBException();
        }


        public void AddCommand(IDbCommand cmd)
        {
            if (stop) throw new WriterStopedException();

            Commands.Enqueue(cmd);
            if (IsSleeping)
            {
                Worker.Interrupt();
            }
        }

        private bool isExecutingQuery;

        internal void Loop(object obj)
        {
            while (!stop)
            {
                try
                {
                    IDbCommand toRun;

                    while (Commands.TryDequeue(out toRun))
                    {
                        isExecutingQuery = true;
                        toRun.Connection = connection;
                        toRun.ExecuteNonQuery();
                        isExecutingQuery = false;
                    }
                    IsSleeping = true;
                    if(!stop)
                    {
                        Thread.Sleep(Timeout.Infinite);
                    }
                    
                }
                
                catch (ThreadInterruptedException ex)
                {
                    IsSleeping = false;
                }
                catch (ThreadAbortException ex)
                {
                    ex = ex;
                }
                catch (Exception ex)
                {
                    isExecutingQuery = false;
                    Stop(true);
                }
                
            }
        }
    }

    [Serializable]
    internal class UnableToWriteAllCommandsToDBException : Exception
    {
        public UnableToWriteAllCommandsToDBException()
        {
        }

        public UnableToWriteAllCommandsToDBException(string message) : base(message)
        {
        }

        public UnableToWriteAllCommandsToDBException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnableToWriteAllCommandsToDBException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    internal class WriterStopedException : Exception
    {
        public WriterStopedException()
        {
        }

        public WriterStopedException(string message) : base(message)
        {
        }

        public WriterStopedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WriterStopedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}