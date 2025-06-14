# Nostrme

- Nostrme is a simple Nostr client built in C# using .NET 8. 
- It connects to a Nostr relay for eg: <a>wss://nos.lol</a> to send and receive events, demonstrating the basics of the Nostr protocol, a decentralized, censorship-resistant communication system. 
- The client sends a text note (kind: 1) to the relay and subscribes to public text notes and metadata (kind: 0) from all users, logging them to the console in real-time.
- [NIP-01](https://github.com/nostr-protocol/nips/blob/master/01.md)