// <copyright file="IntentMatcher.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BotAI.Chat;

using System.Text.RegularExpressions;

/// <summary>
/// Categorized chat intent of a player message.
/// </summary>
public enum ChatIntent
{
    /// <summary>
    /// Player asks for party invitation.
    /// </summary>
    RequestParty,

    /// <summary>
    /// Player asks for free items or zen.
    /// </summary>
    RequestItem,

    /// <summary>
    /// Player says hello or casual greeting.
    /// </summary>
    Greeting,

    /// <summary>
    /// Player asks about bot location or spot.
    /// </summary>
    LocationQuery,

    /// <summary>
    /// Unknown or unhandled intent.
    /// </summary>
    Unknown,
}

/// <summary>
/// Classifies chat messages using Regex pattern matching.
/// </summary>
public class IntentMatcher
{
    private static readonly Regex PartyRegex = new(
        @"(pt|party|xin\s*slot|vào\s*nhóm|cho\s*đi\s*cùng|xin\s*pt|cho\s*pt|vao\s*nhom|cho\s*di\s*cung)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ItemRegex = new(
        @"(xin|cho|vứt|vut)\b.*\b(đồ|do|zen|ngọc|ngoc|bùa|bua|kiếm|kiem|rác|rac|item)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex GreetingRegex = new(
        @"\b(hi|hello|hê\s*lô|he\s*lo|alo|chào|chao|kìa|kia)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex LocationRegex = new(
        @"(ở\s*đâu|o\s*dau|đang\s*đâu|dang\s*dau|map\s*nào|map\s*nao|bãi\s*nào|bai\s*nao|tọa\s*độ|toa\s*do)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Detects chat intent from incoming raw message string.
    /// </summary>
    /// <param name="message">Raw input message.</param>
    /// <returns>Matched <see cref="ChatIntent"/>.</returns>
    public static ChatIntent Classify(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return ChatIntent.Unknown;
        }

        if (PartyRegex.IsMatch(message))
        {
            return ChatIntent.RequestParty;
        }

        if (ItemRegex.IsMatch(message))
        {
            return ChatIntent.RequestItem;
        }

        if (LocationRegex.IsMatch(message))
        {
            return ChatIntent.LocationQuery;
        }

        if (GreetingRegex.IsMatch(message))
        {
            return ChatIntent.Greeting;
        }

        return ChatIntent.Unknown;
    }
}
