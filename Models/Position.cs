using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rendering.Calendar.Models {
    public struct Position {

        public Position(int row, int column, int rowSpan, int columnSpan) {
            this.Row = row;
            this.Column = column;
            this.RowsSpan = rowSpan;
            this.ColumnsSpan = columnSpan;
        }

        public Position(double row, double column, double rowSpan, double columnSpan) {
            this.Row = (int)row;
            this.Column = (int)column;
            this.RowsSpan = (int)rowSpan;
            this.ColumnsSpan = (int)columnSpan;
        }

        public int Row { get; set; }
        public int Column { get; set; }
        public int RowsSpan { get; set; }
        public int ColumnsSpan { get; set; }

        public int FullRow => Row + RowsSpan;
        public int FullColumn => Column + ColumnsSpan;

        public bool IsValidateRow(int row) => row >= Row && row < FullRow;
        public bool IsValidateColumn(int column) => column >= Column && column < FullColumn;

        public bool IsValidateRowSpan(int row, int fullRow) => !(row >= FullRow || fullRow <= Row);
        public bool IsValidateColumnSpan(int column, int fullColumn) => !(column >= FullColumn || fullColumn <= Column);

        public Position Offset(int rowOffset, int columnsOffset) => new Position { Row = this.Row + rowOffset, Column = this.Column + columnsOffset, RowsSpan = this.RowsSpan, ColumnsSpan = this.ColumnsSpan };

        public bool Compare(Position position) => this.Row == position.Row && this.Column == position.Column && this.RowsSpan == position.RowsSpan && this.ColumnsSpan == position.ColumnsSpan;

        public override string ToString() {
            return string.Format("{0}, {1}, {2}, {3}", Row, Column, RowsSpan, ColumnsSpan);
        }
    }
}
