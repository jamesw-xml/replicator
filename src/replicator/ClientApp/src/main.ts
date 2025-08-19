import { createApp } from 'vue';
import App from './App.vue';
import { key, store } from './store';
import { createRouterAsync } from './router';
import installElementPlus from './plugins/element';
import axios from 'axios';

// Add JWT to axios requests if present
axios.interceptors.request.use(config => {
  const token = localStorage.getItem('jwt');
  if (token) {
    config.headers = config.headers || {};
    config.headers['Authorization'] = `Bearer ${token}`;
  }
  return config;
});

async function bootstrap() {
  const router = await createRouterAsync();
const app = createApp(App);
installElementPlus(app);
app.use(store, key).use(router).mount("#app");
}

bootstrap();
