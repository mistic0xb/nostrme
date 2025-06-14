namespace nostrme;

using System.Text.Json;
using System.Net.WebSockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Nostr.Client.Client;
using Nostr.Client.Communicator;
using Nostr.Client.Keys;
using Nostr.Client.Messages;
using Nostr.Client.Messages.Metadata;
using Nostr.Client.Requests;

class Program
{
    private const string RELAY_URL = "wss://nos.lol";
    static async Task Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options =>
        {
            options.ColorBehavior = LoggerColorBehavior.Enabled;
            options.IncludeScopes = false;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        }).SetMinimumLevel(LogLevel.Information));

        var logger = loggerFactory.CreateLogger<Program>();

        var relayURL = new Uri(RELAY_URL);

        using var communicator = new NostrWebsocketCommunicator(relayURL);
        using var client = new NostrWebsocketClient(communicator, null);

        client.Streams.EventStream.Subscribe(res =>
        {
            var ev = res.Event;
            logger.LogInformation("Received [{kind}] from [{pubkey}]: {content}",ev?.Kind, ev?.Pubkey?[..8], ev?.Content);

            if (ev is NostrMetadataEvent evm)
            {
                logger.LogInformation("Name: {name}, about: {about}", evm.Metadata?.Name, evm.Metadata?.About);
            }
        });

        await communicator.Start();

        var privateKey = NostrPrivateKey.GenerateNew();
        Console.WriteLine($"nsec: {privateKey.Bech32}");

        var publicKey = privateKey.DerivePublicKey();
        Console.WriteLine($"npub: {publicKey.Bech32}");

        // create a subs to receive kind:1 events from relay (all public kind:1)
        var filter = new NostrFilter
        {
            Kinds = [NostrKind.ShortTextNote, NostrKind.Metadata], //Filter for kind:1 & Metadata
            Limit = 100    
        };
        var subID = Guid.NewGuid().ToString();
        client.Send(new NostrRequest(subID, filter));
        logger.LogInformation("Subscribed with ID {subscriptionId}:", subID);

        // sending an event to nostr relay
        var note = new NostrEvent
        {
            Kind = NostrKind.ShortTextNote,
            CreatedAt = DateTime.UtcNow,
            Content = "Hello, Nostr! This is a test msg"
        };

        // sign the note with priv-key
        var signedNote = note.Sign(privateKey);

        // json format:
        var options = new JsonSerializerOptions { WriteIndented = true };
        Console.WriteLine($"note: {JsonSerializer.Serialize(note, options)}");
        Console.WriteLine($"signedNote: {JsonSerializer.Serialize(signedNote, options)}");

        // send the event
        client.Send(new NostrEventRequest(signedNote));
        logger.LogInformation("Sent note from {pubkey}: ", publicKey.Bech32);

        // keep the app running
        Console.ReadKey();

        // clean up
        await communicator.Stop(WebSocketCloseStatus.NormalClosure, "closed");
    }
}