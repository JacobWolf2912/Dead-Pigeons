import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { transactionService, boardService, gameService } from '../services/api';
import { BalanceResponse, Board, Game } from '../types';
import './DashboardPage.css';

const DashboardPage: React.FC = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [balance, setBalance] = useState<number | null>(null);
  const [boards, setBoards] = useState<Board[]>([]);
  const [games, setGames] = useState<Game[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'boards' | 'games' | 'deposit'>('boards');

  // Deposit form state
  const [depositAmount, setDepositAmount] = useState<number>(100);
  const [mobilePayId, setMobilePayId] = useState<string>('');
  const [depositError, setDepositError] = useState<string>('');
  const [depositSuccess, setDepositSuccess] = useState<string>('');
  const [depositLoading, setDepositLoading] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<{ [key: string]: string }>({});

  useEffect(() => {
    if (!user?.playerId) {
      setError('Player ID not found. Please log in again.');
      return;
    }

    const fetchData = async () => {
      try {
        setLoading(true);
        setError(null);

        // Fetch balance
        const balanceResponse = await transactionService.getBalance(user.playerId!);
        setBalance((balanceResponse.data as BalanceResponse).balance);

        // Fetch player's boards
        const boardsResponse = await boardService.getMyBoards(user.playerId!);
        setBoards(boardsResponse.data);

        // Fetch games
        const gamesResponse = await gameService.getAllGames();
        setGames(gamesResponse.data);
      } catch (err: any) {
        console.error('Error fetching dashboard data:', err);
        setError('Failed to load dashboard data. Please try again.');
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [user]);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const handlePurchaseBoard = () => {
    navigate('/purchase-board');
  };

  const validateDepositField = (fieldName: string, value: string | number): string => {
    switch (fieldName) {
      case 'amount':
        if (typeof value !== 'number') return 'Invalid amount';
        if (value <= 0) return 'Amount must be greater than 0 DKK';
        if (value < 10) return 'Minimum deposit is 10 DKK';
        if (value > 50000) return 'Maximum deposit is 50,000 DKK';
        if (!Number.isInteger(value)) return 'Amount must be a whole number';
        return '';
      case 'mobilePayId':
        if (!value || typeof value !== 'string') return 'MobilePay ID is required';
        const cleanId = (value as string).trim();
        if (cleanId.length === 0) return 'MobilePay ID is required';
        if (cleanId.length < 5) return 'MobilePay ID must be at least 5 characters';
        if (cleanId.length > 50) return 'MobilePay ID must not exceed 50 characters';
        if (!/^[A-Za-z0-9\-_]+$/.test(cleanId)) {
          return 'MobilePay ID can only contain letters, numbers, hyphens, and underscores';
        }
        return '';
      default:
        return '';
    }
  };

  const handleDepositFieldChange = (fieldName: string, value: string | number) => {
    const error = validateDepositField(fieldName, value);
    setFieldErrors((prev) => ({
      ...prev,
      [fieldName]: error,
    }));

    if (fieldName === 'amount') {
      setDepositAmount(typeof value === 'string' ? parseInt(value) || 0 : value);
    } else if (fieldName === 'mobilePayId') {
      setMobilePayId(value as string);
    }
  };

  const handleDeposit = async () => {
    setDepositError('');
    setDepositSuccess('');

    // Validate all fields
    const amountError = validateDepositField('amount', depositAmount);
    const mobilePayError = validateDepositField('mobilePayId', mobilePayId);

    setFieldErrors({
      amount: amountError,
      mobilePayId: mobilePayError,
    });

    if (amountError || mobilePayError) {
      return;
    }

    try {
      setDepositLoading(true);
      await transactionService.deposit(user!.playerId!, depositAmount, mobilePayId);
      setDepositSuccess(
        `✓ Deposit of ${depositAmount} DKK submitted successfully! Awaiting admin approval.`
      );
      setDepositAmount(100);
      setMobilePayId('');
      setTimeout(() => {
        setDepositSuccess('');
      }, 5000);
    } catch (err: any) {
      const errorMessage = err.response?.data?.error || 'Failed to submit deposit. Please try again.';
      setDepositError(`Deposit failed: ${errorMessage}`);
    } finally {
      setDepositLoading(false);
    }
  };

  if (!user) {
    return <div className="dashboard-error">Please log in to view your dashboard</div>;
  }

  return (
    <div className="dashboard-container">
      <nav className="navbar">
        <h1 className="logo">Dead Pigeons Lottery</h1>
        <div className="nav-info">
          <span className="user-name">{user.fullName}</span>
          <button onClick={() => navigate('/admin')} className="admin-link-btn">
            Admin Panel
          </button>
          <button onClick={handleLogout} className="logout-btn">
            Logout
          </button>
        </div>
      </nav>

      <div className="dashboard-content">
        <div className="sidebar">
          <div className="balance-card">
            <h3>Your Balance</h3>
            {loading ? (
              <p className="loading">Loading...</p>
            ) : (
              <p className="balance-amount">{balance !== null ? `${balance} DKK` : 'N/A'}</p>
            )}
          </div>

          <div className="tabs">
            <button
              className={`tab-btn ${activeTab === 'boards' ? 'active' : ''}`}
              onClick={() => setActiveTab('boards')}
            >
              My Boards
            </button>
            <button
              className={`tab-btn ${activeTab === 'games' ? 'active' : ''}`}
              onClick={() => setActiveTab('games')}
            >
              Games
            </button>
            <button
              className={`tab-btn ${activeTab === 'deposit' ? 'active' : ''}`}
              onClick={() => setActiveTab('deposit')}
            >
              Deposit
            </button>
          </div>
        </div>

        <div className="main-content">
          {error && <div className="error-message">{error}</div>}

          {activeTab === 'boards' && (
            <div className="content-section">
              <h2>My Boards</h2>
              {loading ? (
                <p className="loading">Loading boards...</p>
              ) : boards.length === 0 ? (
                <div className="empty-state">
                  <p>You haven't purchased any boards yet.</p>
                  <button onClick={handlePurchaseBoard} className="purchase-btn">
                    Purchase a Board
                  </button>
                </div>
              ) : (
                <div className="boards-grid">
                  {boards.map((board) => (
                    <div key={board.id} className="board-card">
                      <div className="board-header">
                        <h3>Board {board.fieldCount} Numbers</h3>
                        {board.isWinning && <span className="winning-badge">🎉 WINNING!</span>}
                      </div>
                      <div className="board-info">
                        <p><strong>Price:</strong> {board.price} DKK</p>
                        <p><strong>Numbers:</strong> {board.numbers.join(', ')}</p>
                        <p><strong>Created:</strong> {new Date(board.createdAt).toLocaleDateString()}</p>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {activeTab === 'games' && (
            <div className="content-section">
              <h2>Games</h2>
              {loading ? (
                <p className="loading">Loading games...</p>
              ) : games.length === 0 ? (
                <p>No games available.</p>
              ) : (
                <div className="games-grid">
                  {games.map((game) => (
                    <div key={game.id} className="game-card">
                      <h3>{new Date(game.weekStart).toLocaleDateString()}</h3>
                      <div className="game-info">
                        <p><strong>Draw Time:</strong> {new Date(game.drawTime).toLocaleString()}</p>
                        <p><strong>Status:</strong> {game.isClosed ? '❌ Closed' : '✅ Active'}</p>
                        <p><strong>Boards:</strong> {game.boardCount}</p>
                        {game.winningNumbers && (
                          <p><strong>Winning Numbers:</strong> {game.winningNumbers.number1}, {game.winningNumbers.number2}, {game.winningNumbers.number3}</p>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {activeTab === 'deposit' && (
            <div className="content-section">
              <h2>Deposit Funds</h2>
              <div className="deposit-form">
                {depositError && <div className="error-message">{depositError}</div>}
                {depositSuccess && <div className="success-message">{depositSuccess}</div>}

                <div className="form-group">
                  <label htmlFor="amount">Deposit Amount (DKK)</label>
                  <input
                    id="amount"
                    type="number"
                    min="10"
                    max="50000"
                    step="1"
                    value={depositAmount}
                    onChange={(e) => handleDepositFieldChange('amount', parseInt(e.target.value) || 0)}
                    placeholder="Enter amount (10-50,000 DKK)"
                    className={fieldErrors.amount ? 'input-error' : ''}
                    disabled={depositLoading}
                  />
                  {fieldErrors.amount && (
                    <span className="field-error-message">{fieldErrors.amount}</span>
                  )}
                  {!fieldErrors.amount && depositAmount > 0 && (
                    <span className="field-info-message">
                      Amount: {depositAmount} DKK
                    </span>
                  )}
                </div>

                <div className="form-group">
                  <label htmlFor="mobilePayId">MobilePay Transaction ID</label>
                  <input
                    id="mobilePayId"
                    type="text"
                    value={mobilePayId}
                    onChange={(e) => handleDepositFieldChange('mobilePayId', e.target.value)}
                    placeholder="Enter your MobilePay transaction ID"
                    className={fieldErrors.mobilePayId ? 'input-error' : ''}
                    disabled={depositLoading}
                  />
                  {fieldErrors.mobilePayId && (
                    <span className="field-error-message">{fieldErrors.mobilePayId}</span>
                  )}
                  {!fieldErrors.mobilePayId && mobilePayId && (
                    <span className="field-success-message">✓ Transaction ID format valid</span>
                  )}
                </div>

                <button
                  onClick={handleDeposit}
                  disabled={depositLoading || !!fieldErrors.amount || !!fieldErrors.mobilePayId}
                  className="deposit-btn"
                >
                  {depositLoading ? 'Submitting...' : 'Submit Deposit'}
                </button>

                <div className="deposit-info">
                  <h3>How to Deposit:</h3>
                  <ol>
                    <li>Transfer funds via MobilePay</li>
                    <li>Enter the amount and transaction ID above</li>
                    <li>Click "Submit Deposit"</li>
                    <li>Admin will approve your deposit within 24 hours</li>
                    <li>Your balance will update automatically</li>
                  </ol>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;
