using System.Collections.Concurrent;

namespace eKids.Hubs
{
    public class ConnectionMapping
    {
        private static readonly ConcurrentDictionary<string, string> _connections = new();

        public static void Add(string user, string connectionId)
        {
            _connections[user] = connectionId;
        }

        public static void Remove(string user)
        {
            _connections.TryRemove(user, out _);
        }

        public static string? GetConnectionId(string user)
        {
            _connections.TryGetValue(user, out var connectionId);
            return connectionId;
        }
    }
}
