// Authentication
export interface LoginResponse {
  token: string;
  user: {
    id: string;
    email: string;
    fullName: string;
  };
}

export interface User {
  id: string;
  email: string;
  fullName: string;
  phoneNumber?: string;
  playerId?: string;
  isAdmin?: boolean;
}

// Transactions
export interface Transaction {
  id: string;
  amount: number;
  mobilePayId: string;
  status: 'Pending' | 'Approved';
  createdAt: string;
  approvedAt?: string;
}

export interface BalanceResponse {
  balance: number;
  currency: string;
}

// Boards
export interface Board {
  id: string;
  gameId: string;
  weekStart?: string;
  weekNumber?: string;
  fieldCount: number;
  price: number;
  numbers: number[];
  isWinning?: boolean;
  winningNumbers?: WinningNumbers;
  createdAt: string;
}

export interface PricingInfo {
  prices: {
    fields5: string;
    fields6: string;
    fields7: string;
    fields8: string;
  };
  description: string;
}

// Games
export interface WinningNumbers {
  number1: number;
  number2: number;
  number3: number;
  drawnAt?: string;
}

export interface Game {
  id: string;
  weekStart: string;
  drawTime: string;
  isClosed: boolean;
  winningNumbers?: WinningNumbers;
  boardCount: number;
  winningBoardCount?: number;
}

export interface PendingTransaction {
  id: string;
  playerId: string;
  playerName: string;
  playerEmail: string;
  amount: number;
  mobilePayId: string;
  createdAt: string;
}

export interface WinningBoard {
  id: string;
  playerId: string;
  fieldCount: number;
  price: number;
  numbers: number[];
}
