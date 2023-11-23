namespace API {
	public class Packet {
		public readonly int id;
		public readonly Side side;
		public readonly State state;

		internal readonly object bufferLock = new();

		public Packet(int id, State state, Side side) {
			this.id = id;
			this.state = state;
			this.side = side;
		}
	}
}