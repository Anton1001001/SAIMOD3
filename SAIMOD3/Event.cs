
namespace SAIMOD3;

public class Event
{
    public float Time { get; }
    public EventType Type { get; }

    public Event(float time, EventType type)
    {
        Time = time;
        Type = type;
    }
}