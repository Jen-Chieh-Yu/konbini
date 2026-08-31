<script setup lang="ts">
import { useCartStore } from '../stores/useCartStore'

const cart = useCartStore()
</script>

<template>
  <h2>購物車</h2>
  <p v-if="cart.items.length === 0">
    購物車是空的，<RouterLink to="/products">去逛逛</RouterLink>。
  </p>
  <template v-else>
    <table>
      <thead>
        <tr><th>商品</th><th>單價</th><th>數量</th><th>小計</th><th></th></tr>
      </thead>
      <tbody>
        <tr v-for="item in cart.items" :key="item.productId">
          <td class="name">
            <img v-if="item.imageUrl" :src="item.imageUrl" :alt="item.name" />
            {{ item.name }}
          </td>
          <td>NT$ {{ item.price }}</td>
          <td>
            <button @click="cart.updateQuantity(item.productId, item.quantity - 1)">－</button>
            {{ item.quantity }}
            <button @click="cart.updateQuantity(item.productId, item.quantity + 1)">＋</button>
          </td>
          <td>NT$ {{ item.price * item.quantity }}</td>
          <td><button @click="cart.remove(item.productId)">移除</button></td>
        </tr>
      </tbody>
    </table>
    <div class="summary">
      <p>小計：NT$ {{ cart.subtotal }}</p>
      <p>運費：NT$ {{ cart.deliveryFee }}<small>（滿 500 免運）</small></p>
      <p class="total">合計：NT$ {{ cart.total }}</p>
      <RouterLink to="/checkout" class="checkout">前往結帳</RouterLink>
    </div>
  </template>
</template>

<style scoped>
table { width: 100%; border-collapse: collapse; }
th, td { padding: 0.5rem; border-bottom: 1px solid #eee; text-align: left; }
.name { display: flex; align-items: center; gap: 0.5rem; }
.name img { width: 48px; height: 48px; object-fit: contain; }
.summary { margin-top: 1rem; text-align: right; }
.total { font-size: 1.2rem; font-weight: 700; color: #d33; }
.checkout {
  display: inline-block; padding: 0.5rem 1.5rem; background: #d33; color: #fff;
  border-radius: 4px; text-decoration: none;
}
</style>
