// K.7 — песочница для script-макросов. Код макроса выполняется в <iframe sandbox="allow-scripts">
// БЕЗ allow-same-origin — это ключевая гарантия: у iframe нет доступа к DOM/cookies/localStorage
// родительской страницы, только postMessage. Всё, что макрос может сделать, — вызвать одну из
// функций game.* (roll/chat/getSelectedToken/...), которые внутри iframe — просто обёртки,
// шлющие сообщение родителю и ждущие ответа. Родитель дальше вызывает настоящие Blazor-методы
// через DotNetObjectReference и присылает результат обратно.
window.macroSandbox = (() => {
    const RUNTIME_TIMEOUT_MS = 10000;

    // Код, который будет жить ВНУТРИ песочницы. Строка, а не отдельный файл — iframe грузится
    // через srcdoc, без сетевого запроса и без риска, что кто-то подменит внешний скрипт.
    function buildSrcDoc() {
        return `<!doctype html><html><head><meta charset="utf-8"></head><body><script>
            let nextCallId = 1;
            const pending = new Map();

            window.addEventListener('message', (e) => {
                const msg = e.data;
                if (msg && msg.type === 'call-result' && pending.has(msg.callId)) {
                    const { resolve, reject } = pending.get(msg.callId);
                    pending.delete(msg.callId);
                    if (msg.error) reject(new Error(msg.error)); else resolve(msg.result);
                }
            });

            function callParent(method, args) {
                return new Promise((resolve, reject) => {
                    const callId = nextCallId++;
                    pending.set(callId, { resolve, reject });
                    window.parent.postMessage({ type: 'call', method, args, callId }, '*');
                });
            }

            window.game = {
                roll: (expression, dc, label) => callParent('roll', [expression, dc ?? null, label ?? null]),
                chat: (text) => callParent('chat', [text]),
                getSelectedToken: () => callParent('getSelectedToken', []),
                getTargetToken: () => callParent('getTargetToken', []),
                getTokens: () => callParent('getTokens', []),
                applyDamage: (tokenId, amount, damageType) => callParent('applyDamage', [tokenId, amount, damageType ?? null]),
                applyCondition: (tokenId, slug, value) => callParent('applyCondition', [tokenId, slug, value ?? null]),
                notify: (text) => callParent('notify', [text]),
            };

            window.addEventListener('message', async (e) => {
                if (!e.data || e.data.type !== 'run') return;
                try {
                    const fn = new Function('game', 'return (async () => { ' + e.data.command + ' })();');
                    await fn(window.game);
                    window.parent.postMessage({ type: 'done' }, '*');
                } catch (err) {
                    window.parent.postMessage({ type: 'error', message: String(err && err.message || err) }, '*');
                }
            });
        <\/script></body></html>`;
    }

    // dotNetRef — DotNetObjectReference на Table-компонент; методы MacroRoll/MacroChat/... там
    // помечены [JSInvokable]. command — тело async-функции макроса (без обёртки "async () => {}",
    // её добавляет сам рантайм внутри iframe).
    async function run(command, dotNetRef) {
        return new Promise((resolve) => {
            const iframe = document.createElement('iframe');
            iframe.sandbox = 'allow-scripts';
            iframe.style.display = 'none';
            iframe.srcdoc = buildSrcDoc();

            let settled = false;
            const cleanup = (result) => {
                if (settled) return;
                settled = true;
                clearTimeout(timeoutHandle);
                window.removeEventListener('message', onMessage);
                iframe.remove();
                resolve(result);
            };

            const timeoutHandle = setTimeout(() => cleanup({ ok: false, error: 'Макрос выполнялся дольше 10 секунд — прерван.' }), RUNTIME_TIMEOUT_MS);

            async function onMessage(e) {
                if (e.source !== iframe.contentWindow || !e.data) return;

                if (e.data.type === 'call') {
                    const { method, args, callId } = e.data;
                    try {
                        const result = await dotNetRef.invokeMethodAsync('InvokeMacroApi', method, JSON.stringify(args));
                        iframe.contentWindow.postMessage({ type: 'call-result', callId, result: result ? JSON.parse(result) : null }, '*');
                    } catch (err) {
                        iframe.contentWindow.postMessage({ type: 'call-result', callId, error: String(err && err.message || err) }, '*');
                    }
                    return;
                }

                if (e.data.type === 'done') cleanup({ ok: true });
                else if (e.data.type === 'error') cleanup({ ok: false, error: e.data.message });
            }

            window.addEventListener('message', onMessage);
            document.body.appendChild(iframe);
            iframe.onload = () => iframe.contentWindow.postMessage({ type: 'run', command }, '*');
        });
    }

    return { run };
})();
