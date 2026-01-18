import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_URL || 'http://localhost:5047/api';

// Create axios instance with default config
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add token to requests if it exists
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('authToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// ==================== Authentication ====================
export const authService = {
  register: (email: string, password: string, fullName: string, phoneNumber: string) =>
    apiClient.post('/auth/register', { email, password, fullName, phoneNumber }),

  login: (email: string, password: string) =>
    apiClient.post('/auth/login', { email, password }),

  getCurrentUser: () =>
    apiClient.get('/auth/me'),

  getPendingPlayers: () =>
    apiClient.get('/auth/admin/pending-players'),

  getAllPlayers: () =>
    apiClient.get('/auth/admin/players'),

  approvePendingPlayer: (playerId: string) =>
    apiClient.post(`/auth/admin/approve-player/${playerId}`),

  togglePlayerActive: (playerId: string) =>
    apiClient.post(`/auth/admin/toggle-player/${playerId}/active`),
};

// ==================== Transactions ====================
export const transactionService = {
  deposit: (playerId: string, amount: number, mobilePayTransactionId: string) =>
    apiClient.post('/transactions/deposit', {
      playerId,
      amount,
      mobilePayTransactionId,
    }),

  getBalance: (playerId: string) =>
    apiClient.get('/transactions/balance', { params: { playerId } }),

  getMyTransactions: (playerId: string) =>
    apiClient.get('/transactions/my-transactions', { params: { playerId } }),

  getPendingTransactions: () =>
    apiClient.get('/transactions/pending'),

  approveTransaction: (transactionId: string, amount?: number) =>
    apiClient.post(`/transactions/${transactionId}/approve`, amount !== undefined ? { amount } : {}),

  dismissTransaction: (transactionId: string) =>
    apiClient.post(`/transactions/${transactionId}/dismiss`),
};

// ==================== Boards ====================
export const boardService = {
  purchaseBoard: (playerId: string, gameId: string, fieldCount: number, numbers: number[]) =>
    apiClient.post('/boards/purchase', {
      playerId,
      gameId,
      fieldCount,
      numbers,
    }),

  getMyBoards: (playerId: string) =>
    apiClient.get('/boards/my-boards', { params: { playerId } }),

  getBoard: (boardId: string) =>
    apiClient.get(`/boards/${boardId}`),

  getGameBoards: (gameId: string) =>
    apiClient.get(`/boards/game/${gameId}`),

  getPricing: () =>
    apiClient.get('/boards/pricing'),
};

// ==================== Games ====================
export const gameService = {
  getAllGames: () =>
    apiClient.get('/games'),

  getGame: (gameId: string) =>
    apiClient.get(`/games/${gameId}`),

  getCurrentGame: () =>
    apiClient.get('/games/current'),

  drawWinningNumbers: (gameId: string, number1: number, number2: number, number3: number) =>
    apiClient.post(`/games/${gameId}/draw-numbers`, {
      number1,
      number2,
      number3,
    }),

  getWinningBoards: (gameId: string) =>
    apiClient.get(`/games/${gameId}/winning-boards`),

  createTestGame: () =>
    apiClient.post('/games/create-test-game'),
};

export default apiClient;
