// <copyright file="TemplateChatGenerator.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.BotAI.Chat;

using MUnique.OpenMU.GameLogic.PlugIns.BotAI.Humanization;

/// <summary>
/// Generates varied, natural teencode chat responses for MU Online bots without using LLMs.
/// </summary>
public class TemplateChatGenerator
{
    private static readonly string[] Greetings = ["Alo", "Hế lô", "Hi", "Chào", "Kìa"];
    private static readonly string[] PartyAccept = ["Lên bãi đi tui pt cho", "Vào nhóm đi ông ơi", "Còn slot nè lên mau", "Ok tui pt rồi đó"];
    private static readonly string[] PartyFull = ["Full cmnr ông ơi", "Hết slot rồi để lúc khác nha", "Full pt rồi bạn ơi", "Sorry pt đang full"];
    private static readonly string[] ItemRefuse = ["Tự đi farm đi ông", "Còn mỗi cái nịt nè", "Đồ đâu ra mà cho vãi", "Vừa bán rác hết rồi"];
    private static readonly string[] ItemAccept = ["Cho ít zen nè cầm xài", "Cầm tạm cái này xài đi bro", "Ok trade đi"];
    private static readonly string[] LocationReplies = ["Đang cày bãi rồng nè", "Losttower 7 nha bro", "Tarkan 2 spot cuối", "Lorencia cửa dưới"];

    /// <summary>
    /// Generates a contextual reply message based on intent, party state, and personality.
    /// </summary>
    /// <param name="intent">Chat intent.</param>
    /// <param name="isPartyFull">Whether bot's party is full.</param>
    /// <param name="personality">Bot's personality profile.</param>
    /// <returns>Formatted text message string.</returns>
    public static string GenerateResponse(ChatIntent intent, bool isPartyFull, BotPersonality personality)
    {
        var rng = Random.Shared;
        string greeting = Greetings[rng.Next(Greetings.Length)];

        return intent switch
        {
            ChatIntent.RequestParty => isPartyFull ? $"{greeting}! {PartyFull[rng.Next(PartyFull.Length)]}"
                                                   : $"{greeting}! {PartyAccept[rng.Next(PartyAccept.Length)]}",

            ChatIntent.RequestItem => personality.Sociability > 60
                ? $"{greeting}! {ItemAccept[rng.Next(ItemAccept.Length)]}"
                : $"{greeting}! {ItemRefuse[rng.Next(ItemRefuse.Length)]}",

            ChatIntent.LocationQuery => $"{greeting}! {LocationReplies[rng.Next(LocationReplies.Length)]}",

            ChatIntent.Greeting => $"{greeting} ông ơi, quẩy lên!",

            _ => $"{greeting} bro!",
        };
    }
}
