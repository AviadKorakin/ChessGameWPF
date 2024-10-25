using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessGameWPF.Exceptions
{
    public class ChessException : Exception
    {
        public ChessException(string message) : base(message) { }
    }

    public class CheckException : ChessException
    {
        public CheckException(string message) : base(message) { }
    }

    public class CheckmateException : ChessException
    {
        public CheckmateException(string message) : base(message) { }
    }
    public class GameOverException : ChessException
    {
        public GameOverException(string message) : base(message) { }
    }
    public class IlegalMoveException : ChessException
    {
        public IlegalMoveException(string message) : base(message) { }
    }

    public class PawnException : ChessException
    {
        public PawnException(string message) : base(message) { }
    }

    public class TieException : Exception
    {
        public TieException(string message) : base(message) { }
    }
    public class EnpassantException : ChessException
    {
        public (int Row, int Col) Position { get; }

        public EnpassantException(string message, (int Row, int Col) position)
            : base(message)
        {
            Position = position;
        }
    }
    public class CastlingException : ChessException
    {
        public List<(int Row, int Col)> UpdatedPositions { get; }

        public CastlingException(string message, List<(int Row, int Col)> updatedPositions)
            : base(message)
        {
            UpdatedPositions = updatedPositions;
        }
    }

}
