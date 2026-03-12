namespace MemberTracker.Models.Messaging;

public enum ConversationContextType : byte
{
    General = 1,
    Direct = General,
    Order = 2,
}
