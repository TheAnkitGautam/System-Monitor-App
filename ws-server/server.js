const WebSocket = require('ws');

const server = new WebSocket.Server({
    port: 8080
});

console.log('Realtime server running on ws://localhost:8080');

server.on('connection', socket => {

    console.log('Client connected');

    socket.on('message', message => {

        console.log(
            'Realtime Data:',
            message.toString());

    });

    socket.on('close', () => {

        console.log('Client disconnected');

    });
});