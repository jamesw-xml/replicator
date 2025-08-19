import { createRouter, createWebHistory, RouteRecordRaw, Router } from 'vue-router'
import Home from '../views/Home.vue'

const routes: Array<RouteRecordRaw> = [
  {
    path: '/',
    name: 'Home',
    component: Home,
    meta: { requiresAuth: true }
  },
  {
    path: '/about',
    name: 'About',
    // route level code-splitting
    // this generates a separate chunk (about.[hash].js) for this route
    // which is lazy-loaded when the route is visited.
    component: () => import('../views/About.vue')
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('../views/Login.vue')
  }
]

let authEnabled = true; // default to true

// Fetch auth status from backend
async function fetchAuthStatus() {
  try {
    const res = await fetch('/api/auth/status');
    const data = await res.json();
    authEnabled = data.enabled;
  } catch {
    authEnabled = true; // fallback to true if error
  }
}

export async function createRouterAsync(): Promise<Router> {
  await fetchAuthStatus();

const router = createRouter({
  history: createWebHistory(process.env.BASE_URL),
  routes
  });

  router.beforeEach((to, from, next) => {
    if (!authEnabled) {
      next(); // skip auth checks if auth is disabled
      return;
    }
    const jwt = localStorage.getItem('jwt');
    const isAuthenticated = !!jwt;
    if (to.meta.requiresAuth && !isAuthenticated) {
      next({ path: '/login', query: { redirect: to.fullPath } });
    } else if (to.path === '/login' && isAuthenticated) {
      next('/');
    } else {
      next();
    }
  });

  return router;
}
