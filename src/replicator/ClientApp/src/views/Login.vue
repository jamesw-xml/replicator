<template>
  <div class="login-container">
    <el-card class="login-card">
      <h2>Login</h2>
      <el-form @submit.prevent="onLogin">
        <el-form-item label="Username">
          <el-input v-model="username" autocomplete="username" />
        </el-form-item>
        <el-form-item label="Password">
          <el-input v-model="password" type="password" autocomplete="current-password" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="onLogin">Login</el-button>
        </el-form-item>
        <el-alert v-if="error" type="error" :title="error" show-icon />
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import axios from 'axios';

const username = ref('');
const password = ref('');
const error = ref('');
const loading = ref(false);
const router = useRouter();

const onLogin = async () => {
  error.value = '';
  loading.value = true;
  try {
    const res = await axios.post('/api/auth/login', { username: username.value, password: password.value });
    localStorage.setItem('jwt', res.data.token);
    router.replace('/');
  } catch (e) {
    error.value = 'Invalid username or password';
  } finally {
    loading.value = false;
  }
};
</script>

<style scoped>
.login-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100vh;
}
.login-card {
  width: 350px;
  padding: 30px 20px;
}
</style>
