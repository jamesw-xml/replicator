<template>
  <div id="app">
    <el-container style="border: 1px solid #eee">
      <el-header>
        <template v-if="isAuthenticated">
        <el-menu default-active="1" class="el-menu-demo" mode="horizontal">
          <el-menu-item index="1">
            <router-link to="/">Home</router-link>
          </el-menu-item>
          <el-menu-item index="2" disabled>
            <router-link to="/about">About</router-link>
          </el-menu-item>
            <el-menu-item index="3" style="float:right;">
              <el-button type="text" @click="logout">Logout</el-button>
            </el-menu-item>
        </el-menu>
        </template>
      </el-header>
      <el-main>
        <router-view/>
      </el-main>
    </el-container>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const isAuthenticated = ref(false);

const checkAuth = () => {
  isAuthenticated.value = !!localStorage.getItem('jwt');
};

const logout = () => {
  localStorage.removeItem('jwt');
  isAuthenticated.value = false;
  router.replace('/login');
};

onMounted(() => {
  checkAuth();
  window.addEventListener('storage', checkAuth);
});
</script>

