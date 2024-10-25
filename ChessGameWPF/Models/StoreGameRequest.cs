using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessGameWPF.Models
{
    public class StoreGameRequest
    {
        public int UserId { get; set; }
        public PieceColor UserColor { get; set; }
        public PieceColor ComputerColor { get; set; }
    }
}
