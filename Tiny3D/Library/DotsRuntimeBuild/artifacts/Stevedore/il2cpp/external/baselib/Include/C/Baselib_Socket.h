#pragma once

// Baselib Socket
//
// This is a socket platform abstraction api heavily influenced by non-blocking Berkeley Sockets.
// Berkeley Sockets look like they behave in similar fashion on all platforms, but there are a lot of small differences.
// Compared to Berkeley Sockets this API is somewhat more high level and doesn't provide as fine grained control.
#include "Baselib_ErrorState.h"
#include "Baselib_NetworkAddress.h"
#include "Internal/Baselib_EnumSizeCheck.h"

#ifdef __cplusplus
BASELIB_C_INTERFACE
{
#endif

// Socket Handle, a handle to a specific socket.
typedef struct Baselib_Socket_Handle { intptr_t handle; } Baselib_Socket_Handle;
static const Baselib_Socket_Handle Baselib_Socket_Handle_Invalid = { -1 };

// Socket protocol.
typedef enum
{
    Baselib_Socket_Protocol_UDP = 1,
} Baselib_Socket_Protocol;
BASELIB_ENUM_ENSURE_ABI_COMPATIBILITY(Baselib_Socket_Protocol);

// Socket message. Used to send or receive data in message based protocols such as UDP.
typedef struct Baselib_Socket_Message
{
    Baselib_NetworkAddress*         address;
    void*                           data;
    uint32_t                        dataLen;
} Baselib_Socket_Message;

// Create a socket.
//
// Possible error codes:
// - Baselib_ErrorCode_InvalidArgument:           if context, family or protocol is invalid or unknown.
// - Baselib_ErrorCode_AddressFamilyNotSupported: if the requested address family is not available.
BASELIB_API Baselib_Socket_Handle Baselib_Socket_Create(
    Baselib_NetworkAddress_Family   family,
    Baselib_Socket_Protocol         protocol,
    Baselib_ErrorState*             errorState
);

// Bind socket to a local address and port.
//
// Bind can only be called once per socket.
// Address can either be a specific interface ip address.
// In case if encoded ip is nullptr / "0.0.0.0" / "::" (same as INADDR_ANY) will bind to all interfaces.
//
// \param allowAddressReuse: A set of sockets can be bound to the same address port combination if all
//                           sockets are bound with this flag set to true, similar to SO_REUSEADDR+SO_REUSEPORT.
//                           Please note that setting this flag to false doesn't mean anyone is forbidden to binding to the same ip/port combo,
//                           or in other words it does NOT use SO_EXCLUSIVEADDRUSE where it's available.
//
// Possible error codes:
// - Baselib_ErrorCode_InvalidArgument:    Socket does not represent a valid open socket. Address pointer is null or incompatible.
// - Baselib_ErrorCode_AddressInUse:       Address or port is already bound by another socket, or the system is out of ephemeral ports.
// - Baselib_ErrorCode_AddressUnreachable: Address doesn't map to any known interface.
BASELIB_API void Baselib_Socket_Bind(
    Baselib_Socket_Handle           socket,
    const Baselib_NetworkAddress*   address,
    bool                            allowAddressReuse,
    Baselib_ErrorState*             errorState
);

// Get address of locally bound socket.
//
// Possible error codes:
// - Baselib_ErrorCode_InvalidArgument: Socket does not represent a valid bound socket. Address pointer is null.
BASELIB_API void Baselib_Socket_GetAddress(
    Baselib_Socket_Handle           socket,
    Baselib_NetworkAddress*         address,
    Baselib_ErrorState*             errorState
);

// Send messages to unconnected destinations.
//
// Socket does not need to be bound before calling SendMessages.
// When sending multiple messages an error may be raised after some of the messages were submitted.
//
// If the socket is not already bound to a port SendMessages will implicitly bind the socket before issuing the send operation.
//
// Known issues (behavior may change in the future):
// Some platforms do not support sending zero sized UDP packets.
//
// Possible error codes:
// - Baselib_ErrorCode_AddressUnreachable: Message destination is known to not be reachable from this machine.
// - Baselib_ErrorCode_InvalidArgument:    Socket does not represent a valid socket. Messages is `NULL` or a message has an invalid or incompatible destination.
// - Baselib_ErrorCode_InvalidBufferSize:  Message payload exceeds max message size.
//
// \returns The number of messages successfully sent. This number may be lower than messageCount if send buffer is full or an error was raised. Reported error will be about last message tried to send.
BASELIB_API uint32_t Baselib_Socket_SendMessages(
    Baselib_Socket_Handle           socket,
    Baselib_Socket_Message*         messages,
    uint32_t                        messagesCount,
    Baselib_ErrorState*             errorState
);

// Receive messages from unconnected sources.
//
// UDP message data that doesn't fit a message buffer is silently discarded.
//
// Known issues (behavior may change in the future):
// If the socket is not bound to a port RecvMessages will return zero without raising an error.
// Some platforms does not support receiveing zero sized UDP packets.
//
// Possible error codes:
// - Baselib_ErrorCode_InvalidArgument: Socket does not represent a valid socket. Or messages is `NULL`.
//
// \returns The number of messages successfully received. This number may be lower than messageCount if recv buffer is empty or an error was raised. Reported error will be about last message tried to receive.
BASELIB_API uint32_t Baselib_Socket_RecvMessages(
    Baselib_Socket_Handle           socket,
    Baselib_Socket_Message*         messages,
    uint32_t                        messagesCount,
    Baselib_ErrorState*             errorState
);

// Close socket.
//
// Closing an already closed socket result in a no-op.
BASELIB_API void Baselib_Socket_Close(
    Baselib_Socket_Handle           socket
);

#ifdef __cplusplus
}
#endif
