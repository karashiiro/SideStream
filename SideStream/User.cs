namespace SideStream
{
    public class User
    {
        public string Name { get; }
        public uint Color { get; }

        public User(string username)
        {
            Name = username;
            Color = (uint)username.GetHashCode();
        }
    }
}