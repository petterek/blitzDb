using System;
using System.Collections.Concurrent;
using System.Data;
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
            stop = true;
            if (IsSleeping)
            {
                Worker.Interrupt();
            }
            connection.Close();
        }


        public void AddCommand(IDbCommand cmd)
        {
            Commands.Enqueue(cmd);
            if (IsSleeping)
            {
                Worker.Interrupt();
            }
        }

        internal void Loop(object obj)
        {
            while (!stop)
                try
                {
                    IDbCommand toRun;

                    while(Commands.TryDequeue(out toRun))
                    {

                        toRun.Connection = connection;
                        toRun.ExecuteNonQuery();

                    }
                    IsSleeping = true;
                    Thread.Sleep(Timeout.Infinite);
                }
                catch (ThreadInterruptedException ex)
                {
                    IsSleeping = false;
                }
                catch
                {
                    Stop();
                }
        }
    }
}