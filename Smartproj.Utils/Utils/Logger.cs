using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Smartproj.Utils
{
    public class LoggerEventArgs : EventArgs
    {
        public LoggerEventArgs()
        {
        }
        public LogModeEnum EventsMode { get; set; }
        public object[] Messages { get; set; }
    }
    public class Logger
    {
        private object mSyncRoot;
        private LogModeEnum mLogMode;
        private StreamWriter mWriter;
        public event EventHandler<LoggerEventArgs> Added;
        public bool IsRunning
        {
            get { lock (mSyncRoot) { return mWriter != null; } }
        }
        public LogModeEnum Mode
        {
            get { lock (mSyncRoot) { return mLogMode; } }
            set { lock (mSyncRoot) { mLogMode = value; } }
        }
        public Logger()
        {
            mSyncRoot = new object();
            mLogMode = LogModeEnum.All;
        }
        public void Open(string _logName)
        {
            lock (mSyncRoot)
            {
                if (mWriter == null)
                {
                    mWriter = File.AppendText(_logName);
                    mWriter.AutoFlush = true;
                }
                else
                {
                    throw new InvalidOperationException($"Лог '{_logName}' уже запущен. Необходимо закрыть работающий процесс");
                }
            }
        }
        public bool Close()
        {
            lock (mSyncRoot)
            {
                if (mWriter != null)
                {
                    mWriter.Close();
                    mWriter = null;
                    return true;
                }
                return false;
            }
        }
        protected void WriteLine<T>(LogModeEnum _mode, string _source, params T[] _message)
        {
            lock (mSyncRoot)
            {
                if (mWriter != null)
                {
                    if ((mLogMode & _mode) == _mode)
                    {
                        for (int i = 0; i < _message.Length; i++)
                        {
                            mWriter.WriteLine($"{DateTime.Now}: {_mode}; {_source} => {_message[i]}");
                        }
                    }
                }
            }
            OnAdded(_mode, _message);
        }
        public void Write<T>(LogModeEnum _mode, params T[] _message)
        {
            lock (mSyncRoot)
            {
                if (mWriter != null)
                {
                    if ((mLogMode & _mode) == _mode)
                    {
                        for (int i = 0; i < _message.Length; i++)
                        {
                            mWriter.Write(_message[i]);
                        }
                    }
                }
            }
            OnAdded(_mode, _message);
        }
        public void NewLine()
        {
            lock (mSyncRoot)
            {
                if (mWriter != null)
                {
                    mWriter.WriteLine();
                }
            }
        }
        public void WriteInfo<T>(string _source, params T[] _message)
        {
            WriteLine(LogModeEnum.Message, _source, _message);
        }
        public void WriteWarning<T>(string _source, params T[] _message)
        {
            WriteLine(LogModeEnum.Warning, _source, _message);
        }
        public void WriteError<T>(string _source, params T[] _message)
        {
            WriteLine(LogModeEnum.Error, _source, _message);
        }
        public void WriteAll<T>(string _source, IEnumerable<T> _message, IEnumerable<T> _warnings, IEnumerable<T> _errors)
        {
            lock (mSyncRoot)
            {
                if (mWriter != null)
                {
                    if ((mLogMode & LogModeEnum.Message) == LogModeEnum.Message)
                    {
                        foreach (var message in _message)
                        {
                            mWriter.WriteLine($"{DateTime.Now}: {LogModeEnum.Message}; {_source} => {message}");
                        }
                    }
                    if ((mLogMode & LogModeEnum.Warning) == LogModeEnum.Warning)
                    {
                        foreach (var warning in _warnings)
                        {
                            mWriter.WriteLine($"{DateTime.Now}: {LogModeEnum.Warning}; {_source} => {warning}");
                        }
                    }
                    if ((mLogMode & LogModeEnum.Error) == LogModeEnum.Error)
                    {
                        foreach (var error in _errors)
                        {
                            mWriter.WriteLine($"{DateTime.Now}: {LogModeEnum.Error}; {_source} => {error}");
                        }
                    }
                }
            }

            if (_message.Count() > 0) OnAdded(LogModeEnum.Message, _message);
            if (_warnings.Count() > 0) OnAdded(LogModeEnum.Message, _warnings);
            if (_errors.Count() > 0) OnAdded(LogModeEnum.Error, _errors);
        }
        protected void OnAdded(LogModeEnum _mode, params object[] _message)
        {
            if (Added != null) Added(this, new LoggerEventArgs() { EventsMode = _mode, Messages = _message });
        }
    }

}
