// Adds DOM matchers like toBeInTheDocument(), see https://github.com/testing-library/jest-dom
import '@testing-library/jest-dom/vitest';

// jsdom does not implement matchMedia, which antd's responsive utilities rely on.
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string): MediaQueryList => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  }),
});

// jsdom does not implement ResizeObserver either, antd uses it e.g. for Typography's ellipsis.
class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}
window.ResizeObserver = ResizeObserverStub as unknown as typeof ResizeObserver;

// jsdom lacks MessageChannel, which antd's Select uses to schedule work. Delivers messages asynchronously like a browser.
class MessagePortStub {
  onmessage: ((event: { data: unknown }) => void) | null = null;
  other!: MessagePortStub;
  postMessage(data: unknown) {
    setTimeout(() => this.other.onmessage?.({ data }), 0);
  }
  start() {}
  close() {}
  addEventListener() {}
  removeEventListener() {}
}
class MessageChannelStub {
  port1 = new MessagePortStub();
  port2 = new MessagePortStub();
  constructor() {
    this.port1.other = this.port2;
    this.port2.other = this.port1;
  }
}
window.MessageChannel = MessageChannelStub as unknown as typeof MessageChannel;
