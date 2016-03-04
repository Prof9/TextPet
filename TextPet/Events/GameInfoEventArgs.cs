using LibTextPet.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TextPet.Events {
	public class GameInfoEventArgs : EventArgs {
		public GameInfo GameInfo { get; }

		public GameInfoEventArgs(GameInfo gameInfo) {
			this.GameInfo = gameInfo;
		}
	}
}
