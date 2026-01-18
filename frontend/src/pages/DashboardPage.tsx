import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { transactionService, boardService } from '../services/api';
import { BalanceResponse, Board } from '../types';
import './DashboardPage.css';

const DashboardPage: React.FC = () => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [balance, setBalance] = useState<number | null>(null);
  const [boards, setBoards] = useState<Board[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'boards' | 'resetPassword' | 'balance'>('boards');

  // Deposit form state
  const [mobilePayId, setMobilePayId] = useState<string>('');
  const [depositAmount, setDepositAmount] = useState<string>('');
  const [depositError, setDepositError] = useState<string>('');
  const [depositSuccess, setDepositSuccess] = useState<string>('');
  const [depositLoading, setDepositLoading] = useState(false);
  const [depositFieldErrors, setDepositFieldErrors] = useState<{ [key: string]: string }>({});

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

  const validateDepositField = (fieldName: string, value: string): string => {
    switch (fieldName) {
      case 'mobilePayId':
        if (!value.trim()) return 'Mobile Pay transaction number is required';
        if (value.trim().length < 5) return 'Transaction number must be at least 5 characters';
        return '';
      case 'depositAmount':
        const amount = parseFloat(value);
        if (!value) return 'Amount is required';
        if (isNaN(amount)) return 'Amount must be a number';
        if (amount <= 0) return 'Amount must be greater than 0 DKK';
        if (amount < 10) return 'Minimum deposit is 10 DKK';
        if (amount > 50000) return 'Maximum deposit is 50,000 DKK';
        return '';
      default:
        return '';
    }
  };

  const handleDepositSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setDepositError('');
    setDepositSuccess('');

    // Validate fields
    const mobilePayError = validateDepositField('mobilePayId', mobilePayId);
    const amountError = validateDepositField('depositAmount', depositAmount);

    setDepositFieldErrors({
      mobilePayId: mobilePayError,
      depositAmount: amountError,
    });

    if (mobilePayError || amountError) {
      return;
    }

    try {
      setDepositLoading(true);
      await transactionService.deposit(
        user!.playerId!,
        parseFloat(depositAmount),
        mobilePayId
      );
      setDepositSuccess('✓ Deposit submitted successfully! Awaiting admin approval.');
      setMobilePayId('');
      setDepositAmount('');
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
          <button onClick={handleLogout} className="logout-btn">
            Logout
          </button>
        </div>
      </nav>

      <div className="dashboard-content">
        {/* Left-side vertical tabs */}
        <div className="dashboard-sidebar-tabs">
          <button
            className={`dashboard-tab ${activeTab === 'boards' ? 'active' : ''}`}
            onClick={() => setActiveTab('boards')}
          >
            My Boards
          </button>
          <button
            className={`dashboard-tab ${activeTab === 'resetPassword' ? 'active' : ''}`}
            onClick={() => setActiveTab('resetPassword')}
          >
            Reset Password
          </button>
          <button
            className={`dashboard-tab ${activeTab === 'balance' ? 'active' : ''}`}
            onClick={() => setActiveTab('balance')}
          >
            My Balance
          </button>
        </div>

        <div className="dashboard-main-content">
          {error && <div className="error-message">{error}</div>}

          {/* My Boards Tab */}
          {activeTab === 'boards' && (
            <div className="dashboard-section">
              <h2>My Boards</h2>
              {loading ? (
                <p className="loading">Loading boards...</p>
              ) : (
                <div className="boards-grid">
                  {boards.map((board) => (
                    <div key={board.id} className="board-card">
                      <div className="board-grid">
                        {Array.from({ length: 16 }, (_, i) => i + 1).map((num) => (
                          <div
                            key={num}
                            className={`board-number ${
                              board.numbers.includes(num) ? 'selected' : ''
                            }`}
                          >
                            {num}
                          </div>
                        ))}
                      </div>
                    </div>
                  ))}
                  {/* New Board Button */}
                  <button
                    className="board-card new-board-btn"
                    onClick={handlePurchaseBoard}
                  >
                    <div className="new-board-content">
                      <div className="plus-sign">+</div>
                      <div className="new-board-text">New Board</div>
                    </div>
                  </button>
                </div>
              )}
              {boards.length === 0 && !loading && (
                <p className="empty-state-text">You haven't purchased any boards yet for this lottery.</p>
              )}
            </div>
          )}

          {/* Reset Password Tab */}
          {activeTab === 'resetPassword' && (
            <div className="dashboard-section">
              <h2>Reset Password</h2>
              <div className="reset-password-placeholder">
                <p>Password reset functionality coming soon.</p>
              </div>
            </div>
          )}

          {/* My Balance Tab */}
          {activeTab === 'balance' && (
            <div className="dashboard-section">
              <h2>My Balance</h2>
              {loading ? (
                <p className="loading">Loading balance...</p>
              ) : (
                <>
                  <div className="balance-display">
                    <p className="balance-label">Current Balance</p>
                    <p className="balance-amount">
                      {balance !== null ? `${balance} DKK` : 'N/A'}
                    </p>
                  </div>

                  {/* Deposit Form */}
                  <div className="deposit-form-section">
                    <h3>Add Funds</h3>
                    {depositError && (
                      <div className="error-message">{depositError}</div>
                    )}
                    {depositSuccess && (
                      <div className="success-message">{depositSuccess}</div>
                    )}
                    <form onSubmit={handleDepositSubmit} className="deposit-form">
                      <div className="form-group">
                        <label htmlFor="mobilePayId">Mobile Pay Transaction Number</label>
                        <input
                          id="mobilePayId"
                          type="text"
                          value={mobilePayId}
                          onChange={(e) => {
                            setMobilePayId(e.target.value);
                            setDepositFieldErrors((prev) => ({
                              ...prev,
                              mobilePayId: '',
                            }));
                          }}
                          placeholder="Enter transaction number"
                          disabled={depositLoading}
                          className={depositFieldErrors.mobilePayId ? 'input-error' : ''}
                        />
                        {depositFieldErrors.mobilePayId && (
                          <span className="field-error-message">
                            {depositFieldErrors.mobilePayId}
                          </span>
                        )}
                      </div>

                      <div className="form-group">
                        <label htmlFor="depositAmount">Amount (DKK)</label>
                        <input
                          id="depositAmount"
                          type="number"
                          value={depositAmount}
                          onChange={(e) => {
                            setDepositAmount(e.target.value);
                            setDepositFieldErrors((prev) => ({
                              ...prev,
                              depositAmount: '',
                            }));
                          }}
                          placeholder="10 - 50,000"
                          min="10"
                          max="50000"
                          disabled={depositLoading}
                          className={depositFieldErrors.depositAmount ? 'input-error' : ''}
                        />
                        {depositFieldErrors.depositAmount && (
                          <span className="field-error-message">
                            {depositFieldErrors.depositAmount}
                          </span>
                        )}
                      </div>

                      <button
                        type="submit"
                        disabled={depositLoading}
                        className="deposit-btn"
                      >
                        {depositLoading ? 'Submitting...' : 'Submit Deposit'}
                      </button>
                    </form>
                  </div>
                </>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default DashboardPage;
