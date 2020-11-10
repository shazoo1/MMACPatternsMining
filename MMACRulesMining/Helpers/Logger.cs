using System;
using System.Collections.Generic;
using System.Text;

namespace MMACRulesMining
{
    /// <summary>
    /// Simple logger.
    /// </summary>
    public class Logger
    {
        private static Logger _instance;

        private readonly DateTime startTime;
        private DateTime lastLap = DateTime.MinValue;

        private Logger()
        {
            startTime = DateTime.Now;
        }

        public static Logger GetInstance()
        {
            if (_instance == null)
                _instance = new Logger();
            return _instance;
        }

        /// <summary>
        /// Prints message with timestamp.
        /// </summary>
        /// <param name="text"></param>
        public void PrintMilestone(string text)
        {
            Console.WriteLine(string.Format("------- {0}: {1}", DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff"), text));
        }

        /// <summary>
        /// Prints message with timespan from the beginning of main method.
        /// </summary>
        /// <param name="text"></param>
        public void PrintExecutionTime(string text)
        {
            Console.WriteLine(string.Format("{0}: {1}", (DateTime.Now - startTime).ToString(), text));
        }

        /// <summary>
        /// Prints message with time since last method call (or 0).
        /// </summary>
        /// <param name="text"></param>
        public void PrintLap(string text)
        {
            TimeSpan difference = TimeSpan.Zero;
            if (!(lastLap == DateTime.MinValue))
            {
                // Print 0 and set the timer up.
                difference = DateTime.Now - lastLap;
                BeginLap();
            }
            Console.WriteLine(string.Format("{0} - {1}", text, difference.ToString()));
        }

        /// <summary>
        /// Sets time of the lap to current.
        /// </summary>
        public void BeginLap()
        {
            lastLap = DateTime.Now;
        }
    }
}
