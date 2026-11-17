<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { OrderService } from '../api/services/OrderService'
import type { OrderDTO } from '../api/interfaces/OrderDTO'
import AppStatePanel from '@shared/components/AppStatePanel.vue'
import { formatMoney } from '@shared/constants/catalog'
import { formatDateTime } from '@shared/utils/formatDateTime'

const route = useRoute()
const order = ref<OrderDTO | null>(null)
/** not-found：不存在、不是自己的訂單或網址打錯；error：連線或伺服器問題，可以重試 */
const status = ref<'loading' | 'ready' | 'not-found' | 'error'>('loading')

async function load() {
  const raw = String(route.params.id)
  // 不是正整數的網址（/orders/abc）後端也對不到，不必送請求
  if (!/^[1-9]\d*$/.test(raw)) {
    status.value = 'not-found'
    return
  }
  status.value = 'loading'
  try {
    order.value = await OrderService.getOrder(Number(raw))
    status.value = order.value ? 'ready' : 'not-found'
  } catch {
    status.value = 'error'
  }
}

// 離開本頁時 params 也會變（id 變成 undefined），要擋掉，否則會多發一次請求
watch(
  () => route.params.id,
  () => {
    if (route.name === 'OrderDetail') load()
  },
)
// 未登入的人進不來（路由守衛），這裡直接載入
onMounted(load)
</script>

<template>
  <div v-if="status === 'loading'" v-loading="true" class="detail-loading" aria-busy="true" />
  <AppStatePanel
    v-else-if="status === 'not-found'"
    title="找不到這筆訂單"
    description="訂單編號可能有誤，請從「我的訂單」重新選擇。"
  >
    <RouterLink to="/orders"><el-button type="primary">回我的訂單</el-button></RouterLink>
  </AppStatePanel>
  <AppStatePanel
    v-else-if="status === 'error'"
    title="暫時看不到這筆訂單"
    description="連線好像出了點問題，稍等一下再重新載入。"
  >
    <el-button type="primary" plain @click="load">重新載入</el-button>
  </AppStatePanel>
  <article v-else-if="order" class="order-detail">
    <RouterLink to="/orders" class="back-link">‹ 我的訂單</RouterLink>
    <header class="detail-head">
      <h2>訂單 #{{ order.id }}</h2>
      <time :datetime="order.createdAt">{{ formatDateTime(order.createdAt) }}</time>
    </header>

    <el-row :gutter="20">
      <el-col :xs="24" :md="15">
        <el-card shadow="never" class="panel">
          <template #header>商品明細</template>
          <ul class="items">
            <li v-for="item in order.items" :key="item.productId" class="item">
              <img
                v-if="item.imageUrl"
                :src="item.imageUrl"
                :alt="item.productName"
                class="item-img"
                loading="lazy"
              />
              <div v-else class="item-img" aria-hidden="true" />
              <div class="item-body">
                <RouterLink :to="`/products/${item.productId}`" class="item-name">
                  {{ item.productName }}
                </RouterLink>
                <span class="item-meta">
                  NT$ {{ formatMoney(item.unitPrice) }} × {{ item.quantity }}
                </span>
              </div>
              <span class="item-subtotal">NT$ {{ formatMoney(item.subtotal) }}</span>
            </li>
          </ul>
        </el-card>
      </el-col>
      <el-col :xs="24" :md="9">
        <el-card shadow="never" class="panel">
          <template #header>金額</template>
          <dl class="summary">
            <div>
              <dt>小計</dt>
              <dd>NT$ {{ formatMoney(order.subtotal) }}</dd>
            </div>
            <div>
              <dt>運費</dt>
              <dd>NT$ {{ formatMoney(order.deliveryFee) }}</dd>
            </div>
            <div class="total">
              <dt>合計</dt>
              <dd>NT$ {{ formatMoney(order.total) }}</dd>
            </div>
          </dl>
        </el-card>
        <el-card shadow="never" class="panel">
          <template #header>收件資訊</template>
          <el-descriptions :column="1">
            <el-descriptions-item label="聯絡人">{{ order.contactName }}</el-descriptions-item>
            <el-descriptions-item label="電話">{{ order.contactPhone }}</el-descriptions-item>
            <el-descriptions-item label="寄送地址">{{
              order.deliveryAddress
            }}</el-descriptions-item>
            <el-descriptions-item v-if="order.memo" label="備註">{{
              order.memo
            }}</el-descriptions-item>
          </el-descriptions>
        </el-card>
      </el-col>
    </el-row>
  </article>
</template>

<style scoped>
.detail-loading {
  min-height: 240px;
}
.back-link {
  display: inline-flex;
  align-items: center;
  min-height: var(--kb-tap);
  color: var(--kb-muted);
}
.detail-head {
  display: flex;
  flex-wrap: wrap;
  align-items: baseline;
  gap: 0.25rem 1rem;
  margin-bottom: 1rem;
}
.detail-head h2 {
  margin: 0;
}
.detail-head time {
  color: var(--kb-muted);
}
.panel {
  margin-bottom: 1rem;
}
.items {
  list-style: none;
  margin: 0;
  padding: 0;
}
.item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 0;
  border-bottom: 1px solid var(--kb-line);
}
.item:last-child {
  border-bottom: 0;
}
.item-img {
  flex: none;
  width: 64px;
  height: 64px;
  object-fit: cover;
  border-radius: var(--kb-radius-img);
  background: var(--kb-surface-2);
}
.item-body {
  flex: 1;
  min-width: 0;
  display: grid;
  gap: 0.2rem;
}
.item-name {
  color: var(--kb-ink);
  font-weight: 600;
}
.item-meta {
  color: var(--kb-muted);
  font-size: 0.875rem;
}
.item-subtotal {
  font-weight: 600;
  white-space: nowrap;
}
.summary {
  margin: 0;
}
.summary > div {
  display: flex;
  justify-content: space-between;
  padding: 0.3rem 0;
}
.summary dd {
  margin: 0;
}
.summary .total {
  margin-top: 0.4rem;
  padding-top: 0.6rem;
  border-top: 1px solid var(--kb-line);
  font-weight: 700;
  font-size: 1.1rem;
}
</style>
