using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuickRaidButtons
{
    public class QRBException : ApplicationException
    {

        public QRBException()
            : base()
        {
        }

        public QRBException( string message )
            : base( message )
        {
        }

        public QRBException( string message, Exception inner )
            : base( message, inner )
        {
        }
    }
}
