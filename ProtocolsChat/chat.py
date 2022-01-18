#!/usr/bin/env python3
import json

from aiohttp import web, WSMsgType


class ChatInteractor(web.View):
    def __init__(self, request):
        super().__init__(request)
        self.ws = web.WebSocketResponse()
        self.userid = ''
        self._websockets = None
        self._reacts = {'INIT': self._react_on_init_msg,
                        'TEXT': self._react_on_text_msg}

    async def _react_on_init_msg(self, msg_json):
        if 'websockets' not in self.request.app:
            self.request.app['websockets'] = dict()

        self._websockets = self.request.app['websockets']
        self.userid = msg_json['id']
        _user_enter_info = {'mtype': 'USER_ENTER', 'id': self.userid}

        for ws in self._websockets.values():
            await ws.send_json(_user_enter_info)

        self._websockets[self.userid] = self.ws

    async def _react_on_text_msg(self, msg_json):
        _id = msg_json['id']
        _text = msg_json['text']
        _to = (msg_json['to'],)
        _msg_json = {'mtype': 'DM', 'id': _id, 'text': _text}

        if not any(_to):
            _msg_json['mtype'] = 'MSG'
            _to = (ws for ws in self._websockets if ws != _id)

        for ws in _to:
            if ws in self._websockets:
                await self._websockets[ws].send_json(_msg_json)

    async def _react_on_msg(self, msg):
        if msg.data == 'ping':
            await self.ws.pong(b'pong')
            return

        json_msg = json.loads(msg.data)
        await self._reacts[json_msg['mtype']](json_msg)

    async def get(self):
        await self.ws.prepare(self.request)

        async for msg in self.ws:
            if msg.type == WSMsgType.TEXT:
                await self._react_on_msg(msg)

        self._websockets.pop(self.userid)
        _leave_msg = {'mtype': 'USER_LEAVE', 'id': self.userid}
        for ws in self._websockets.values():
            await ws.send_json(_leave_msg)

        await self.ws.close()
        return self.ws


class WSChat:
    def __init__(self, host='0.0.0.0', port=8080):
        self.host = host
        self.port = port
        self.conns = {}

    @staticmethod
    async def main_page(request):
        return web.FileResponse('./index.html')

    def run(self):
        app = web.Application()
        app.router.add_get('/', self.main_page)
        app.router.add_get('/chat', ChatInteractor)
        web.run_app(app, host=self.host, port=self.port)


if __name__ == '__main__':
    WSChat().run()
