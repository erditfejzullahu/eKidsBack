using System.Collections.Concurrent;

namespace eKids.Hubs
{
    public class ConnectionMapping
    {
        private readonly ConcurrentDictionary<string, string> _connections = new();

        public void Add(string user, string connectionId)
        {
            _connections[user] = connectionId;
        }

        public void Remove(string user)
        {
            _connections.TryRemove(user, out _);
        }

        public string? GetConnectionId(string user)
        {
            _connections.TryGetValue(user, out var connectionId);
            return connectionId;
        }
    }
}
