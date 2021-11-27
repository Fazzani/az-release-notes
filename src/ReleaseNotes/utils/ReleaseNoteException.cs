using System;

namespace ReleaseNotes.utils
{
    internal class ReleaseNoteException: Exception
    {
        public ReleaseNoteException(string message): base(message)
        {
        }

        public ReleaseNoteException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
