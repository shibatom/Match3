

using System;

namespace Internal.Scripts.MapScripts
{
    public class GetLevelReachedNumberAndEventArgs : EventArgs
    {
        public int Number { get; private set; }

        public GetLevelReachedNumberAndEventArgs(int number)
        {
            Number = number;
        }
    }
}