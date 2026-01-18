import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { transactionService, gameService, authService } from '../services/api';
import { PendingTransaction, Game } from '../types';
import './AdminPanel.css';

interface PendingPlayer {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  createdAt: string;
}

interface Player {
  id: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  isActive: boolean;
  createdAt: string;
}

const AdminPanel: React.FC = () => {
  const navigate = useNavigate();
  const { logout } = useAuth();
  const [activeTab, setActiveTab] = useState<'pendingPlayers' | 'players' | 'transactions' | 'games'>('pendingPlayers');
  const [pendingPlayers, setPendingPlayers] = useState<PendingPlayer[]>([]);
  const [approvedPlayers, setApprovedPlayers] = useState<Player[]>([]);
  const [pendingTransactions, setPendingTransactions] = useState<PendingTransaction[]>([]);
  const [games, setGames] = useState<Game[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [approving, setApproving] = useState<string | null>(null);
  const [dismissing, setDismissing] = useState<string | null>(null);
  const [drawingNumbers, setDrawingNumbers] = useState<string | null>(null);
  const [expandedPlayer, setExpandedPlayer] = useState<string | null>(null);
  const [toggling, setToggling] = useState<string | null>(null);
  const [editedAmounts, setEditedAmounts] = useState<{ [key: string]: number }>({});

  // Draw numbers form state
  const [selectedGameId, setSelectedGameId] = useState<string>('');
  const [number1, setNumber1] = useState<number>(1);
  const [number2, setNumber2] = useState<number>(2);
  const [number3, setNumber3] = useState<number>(3);
  const [numberErrors, setNumberErrors] = useState<{ [key: string]: string }>({});

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      setError(null);

      const [pendingPlayersRes, playersRes, transRes, gamesRes] = await Promise.all([
        authService.getPendingPlayers(),
        authService.getAllPlayers(),
        transactionService.getPendingTransactions(),
        gameService.getAllGames(),
      ]);

      setPendingPlayers(pendingPlayersRes.data.pendingPlayers);
      setApprovedPlayers(playersRes.data.players);
      setPendingTransactions(transRes.data);
      setGames(gamesRes.data);

      if (gamesRes.data.length > 0) {
        setSelectedGameId(gamesRes.data[0].id);
      }
    } catch (err: any) {
      console.error('Error fetching admin data:', err);
      setError('Failed to load admin data. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleApproveTransaction = async (transactionId: string, originalAmount: number) => {
    try {
      setApproving(transactionId);
      const approveAmount = editedAmounts[transactionId] || originalAmount;
      await transactionService.approveTransaction(transactionId, approveAmount);

      // Remove from list
      setPendingTransactions((prev) =>
        prev.filter((t) => t.id !== transactionId)
      );

      // Clear edited amount
      setEditedAmounts((prev) => {
        const updated = { ...prev };
        delete updated[transactionId];
        return updated;
      });

      setApproving(null);
    } catch (err: any) {
      console.error('Error approving transaction:', err);
      setError('Failed to approve transaction. Please try again.');
      setApproving(null);
    }
  };

  const handleDismissTransaction = async (transactionId: string) => {
    try {
      setDismissing(transactionId);
      await transactionService.dismissTransaction(transactionId);

      // Remove from list
      setPendingTransactions((prev) =>
        prev.filter((t) => t.id !== transactionId)
      );

      // Clear edited amount
      setEditedAmounts((prev) => {
        const updated = { ...prev };
        delete updated[transactionId];
        return updated;
      });

      setDismissing(null);
    } catch (err: any) {
      console.error('Error dismissing transaction:', err);
      setError('Failed to dismiss transaction. Please try again.');
      setDismissing(null);
    }
  };

  const validateNumberInputs = (): boolean => {
    const errors: { [key: string]: string } = {};

    // Check if numbers are in valid range
    if (number1 < 1 || number1 > 16) errors.number1 = 'Must be between 1-16';
    if (number2 < 1 || number2 > 16) errors.number2 = 'Must be between 1-16';
    if (number3 < 1 || number3 > 16) errors.number3 = 'Must be between 1-16';

    // Check for duplicates
    const numbers = [number1, number2, number3];
    const uniqueNumbers = new Set(numbers);
    if (uniqueNumbers.size !== 3) {
      setError('Draw numbers error: All three numbers must be unique');
      setNumberErrors({
        duplicate: 'Duplicate numbers detected',
      });
      return false;
    }

    setNumberErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleDrawNumbers = async () => {
    setError(null);

    if (!selectedGameId) {
      setError('Game selection required: Please select a game before drawing numbers');
      return;
    }

    // Get selected game to check if already closed
    const selectedGame = games.find((g) => g.id === selectedGameId);
    if (selectedGame?.isClosed) {
      setError('Game already closed: Cannot draw numbers for a game that has already been drawn');
      return;
    }

    if (!validateNumberInputs()) {
      return;
    }

    try {
      setDrawingNumbers(selectedGameId);
      await gameService.drawWinningNumbers(selectedGameId, number1, number2, number3);

      // Refresh games list
      await fetchData();
      setDrawingNumbers(null);
      setError(null);
      setNumberErrors({});
    } catch (err: any) {
      const errorMsg = err.response?.data?.error || 'Failed to draw numbers. Please try again.';
      setError(`Draw operation failed: ${errorMsg}`);
      setDrawingNumbers(null);
    }
  };

  const handleNumberChange = (numberKey: string, value: number) => {
    const numValue = Math.max(1, Math.min(16, value)); // Clamp between 1-16

    switch (numberKey) {
      case 'number1':
        setNumber1(numValue);
        break;
      case 'number2':
        setNumber2(numValue);
        break;
      case 'number3':
        setNumber3(numValue);
        break;
    }

    // Clear specific error for this field
    if (numberErrors[numberKey]) {
      setNumberErrors((prev) => {
        const updated = { ...prev };
        delete updated[numberKey];
        return updated;
      });
    }
  };

  const handleApprovePendingPlayer = async (playerId: string) => {
    try {
      setApproving(playerId);
      await authService.approvePendingPlayer(playerId);

      // Remove from pending and refresh data
      setPendingPlayers((prev) => prev.filter((p) => p.id !== playerId));
      setApproving(null);
      setError(null);
    } catch (err: any) {
      console.error('Error approving player:', err);
      setError('Failed to approve player. Please try again.');
      setApproving(null);
    }
  };

  const handleTogglePlayerActive = async (playerId: string) => {
    try {
      setToggling(playerId);
      await authService.togglePlayerActive(playerId);

      // Update player status
      setApprovedPlayers((prev) =>
        prev.map((p) =>
          p.id === playerId ? { ...p, isActive: !p.isActive } : p
        )
      );
      setToggling(null);
      setError(null);
    } catch (err: any) {
      console.error('Error toggling player status:', err);
      setError('Failed to toggle player status. Please try again.');
      setToggling(null);
    }
  };

  return (
    <div className="admin-container">
      <nav className="admin-navbar">
        <h1 className="admin-logo">Dead Pigeons - Admin Panel</h1>
        <button
          onClick={() => {
            logout();
            navigate('/login');
          }}
          className="back-btn"
        >
          Logout
        </button>
      </nav>

      <div className="admin-content">
        <div className="admin-sidebar-tabs">
          <button
            className={`admin-tab ${activeTab === 'pendingPlayers' ? 'active' : ''}`}
            onClick={() => setActiveTab('pendingPlayers')}
          >
            Pending Players
          </button>
          <button
            className={`admin-tab ${activeTab === 'players' ? 'active' : ''}`}
            onClick={() => setActiveTab('players')}
          >
            Players
          </button>
          <button
            className={`admin-tab ${activeTab === 'transactions' ? 'active' : ''}`}
            onClick={() => setActiveTab('transactions')}
          >
            Transactions
          </button>
          <button
            className={`admin-tab ${activeTab === 'games' ? 'active' : ''}`}
            onClick={() => setActiveTab('games')}
          >
            Draw Numbers
          </button>
        </div>

        {error && <div className="error-message">{error}</div>}

        <div className="admin-main-content">
          {activeTab === 'pendingPlayers' && (
            <div className="admin-section">
              <h2>Pending Players</h2>
              {loading ? (
                <p className="loading">Loading pending players...</p>
              ) : pendingPlayers.length === 0 ? (
                <p className="empty-state">No pending players.</p>
              ) : (
                <div className="players-grid">
                  {pendingPlayers.map((player) => (
                    <div
                      key={player.id}
                      className={`player-card ${expandedPlayer === player.id ? 'expanded' : ''}`}
                    >
                      <div
                        className="player-card-header"
                        onClick={() => setExpandedPlayer(expandedPlayer === player.id ? null : player.id)}
                      >
                        <div className="player-icon">👤</div>
                        <div className="player-name">{player.fullName}</div>
                      </div>
                      {expandedPlayer === player.id && (
                        <div className="player-card-details">
                          <p><strong>Full Name:</strong> {player.fullName}</p>
                          <p><strong>Email:</strong> {player.email}</p>
                          <p><strong>Phone:</strong> {player.phoneNumber}</p>
                          <p><strong>Registered:</strong> {new Date(player.createdAt).toLocaleDateString()}</p>
                          <button
                            onClick={() => handleApprovePendingPlayer(player.id)}
                            disabled={approving === player.id}
                            className="approve-btn"
                          >
                            {approving === player.id ? 'Approving...' : 'Approve Player'}
                          </button>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {activeTab === 'players' && (
            <div className="admin-section">
              <h2>Approved Players</h2>
              {loading ? (
                <p className="loading">Loading players...</p>
              ) : approvedPlayers.length === 0 ? (
                <p className="empty-state">No approved players.</p>
              ) : (
                <div className="players-grid">
                  {approvedPlayers.map((player) => (
                    <div
                      key={player.id}
                      className={`player-card ${expandedPlayer === player.id ? 'expanded' : ''}`}
                    >
                      <div
                        className="player-card-header"
                        onClick={() => setExpandedPlayer(expandedPlayer === player.id ? null : player.id)}
                      >
                        <div className="player-icon">👤</div>
                        <div className="player-name">{player.fullName}</div>
                      </div>
                      {expandedPlayer === player.id && (
                        <div className="player-card-details">
                          <p><strong>Full Name:</strong> {player.fullName}</p>
                          <p><strong>Email:</strong> {player.email}</p>
                          <p><strong>Phone:</strong> {player.phoneNumber}</p>
                          <p><strong>Status:</strong> {player.isActive ? '✅ Active' : '⛔ Inactive'}</p>
                          <p><strong>Registered:</strong> {new Date(player.createdAt).toLocaleDateString()}</p>
                          <button
                            onClick={() => handleTogglePlayerActive(player.id)}
                            disabled={toggling === player.id}
                            className={`toggle-btn ${player.isActive ? 'active' : 'inactive'}`}
                          >
                            {toggling === player.id
                              ? 'Updating...'
                              : player.isActive
                              ? 'Deactivate Player'
                              : 'Activate Player'}
                          </button>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          )}

          {activeTab === 'transactions' && (
          <div className="admin-section">
            <h2>Pending Transactions</h2>
            {loading ? (
              <p className="loading">Loading transactions...</p>
            ) : pendingTransactions.length === 0 ? (
              <p className="empty-state">No pending transactions.</p>
            ) : (
              <div className="transactions-list">
                {pendingTransactions.map((trans) => (
                  <div key={trans.id} className="transaction-item">
                    <div className="transaction-details">
                      <div className="transaction-row">
                        <label>Player Name:</label>
                        <span>{trans.playerName || 'N/A'}</span>
                      </div>
                      <div className="transaction-row">
                        <label>Email:</label>
                        <span>{trans.playerEmail}</span>
                      </div>
                      <div className="transaction-row">
                        <label>Mobile Pay ID:</label>
                        <span className="read-only-field">{trans.mobilePayId}</span>
                      </div>
                      <div className="transaction-row">
                        <label>Amount (DKK):</label>
                        <input
                          type="number"
                          value={editedAmounts[trans.id] || trans.amount}
                          onChange={(e) =>
                            setEditedAmounts((prev) => ({
                              ...prev,
                              [trans.id]: parseFloat(e.target.value) || 0,
                            }))
                          }
                          className="amount-input"
                          min="0.01"
                          step="0.01"
                        />
                      </div>
                      <div className="transaction-row">
                        <label>Date:</label>
                        <span>{new Date(trans.createdAt).toLocaleDateString()}</span>
                      </div>
                    </div>
                    <div className="transaction-actions">
                      <button
                        onClick={() => handleApproveTransaction(trans.id, trans.amount)}
                        disabled={approving === trans.id || dismissing === trans.id}
                        className="approve-btn"
                      >
                        {approving === trans.id ? 'Approving...' : 'Approve'}
                      </button>
                      <button
                        onClick={() => handleDismissTransaction(trans.id)}
                        disabled={approving === trans.id || dismissing === trans.id}
                        className="dismiss-btn"
                      >
                        {dismissing === trans.id ? 'Dismissing...' : 'Dismiss'}
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {activeTab === 'games' && (
          <div className="admin-section">
            <h2>Draw Winning Numbers</h2>
            <div className="draw-form">
              <div className="form-group">
                <label htmlFor="game-select">Select Game</label>
                <select
                  id="game-select"
                  value={selectedGameId}
                  onChange={(e) => setSelectedGameId(e.target.value)}
                  className="form-select"
                >
                  <option value="">Choose a game...</option>
                  {games.map((game) => (
                    <option key={game.id} value={game.id} disabled={game.isClosed}>
                      Week of {new Date(game.weekStart).toLocaleDateString()}
                      {game.isClosed ? ' (Closed)' : ''}
                    </option>
                  ))}
                </select>
              </div>

              <div className="numbers-input">
                <div className="form-group">
                  <label htmlFor="number1">Number 1 (1-16)</label>
                  <input
                    id="number1"
                    type="number"
                    min="1"
                    max="16"
                    value={number1}
                    onChange={(e) => handleNumberChange('number1', parseInt(e.target.value) || 1)}
                    className={`number-input ${numberErrors.number1 || numberErrors.duplicate ? 'input-error' : ''}`}
                  />
                  {numberErrors.number1 && (
                    <span className="field-error-message">{numberErrors.number1}</span>
                  )}
                </div>

                <div className="form-group">
                  <label htmlFor="number2">Number 2 (1-16)</label>
                  <input
                    id="number2"
                    type="number"
                    min="1"
                    max="16"
                    value={number2}
                    onChange={(e) => handleNumberChange('number2', parseInt(e.target.value) || 2)}
                    className={`number-input ${numberErrors.number2 || numberErrors.duplicate ? 'input-error' : ''}`}
                  />
                  {numberErrors.number2 && (
                    <span className="field-error-message">{numberErrors.number2}</span>
                  )}
                </div>

                <div className="form-group">
                  <label htmlFor="number3">Number 3 (1-16)</label>
                  <input
                    id="number3"
                    type="number"
                    min="1"
                    max="16"
                    value={number3}
                    onChange={(e) => handleNumberChange('number3', parseInt(e.target.value) || 3)}
                    className={`number-input ${numberErrors.number3 || numberErrors.duplicate ? 'input-error' : ''}`}
                  />
                  {numberErrors.number3 && (
                    <span className="field-error-message">{numberErrors.number3}</span>
                  )}
                </div>
                {numberErrors.duplicate && (
                  <span className="field-error-message" style={{ gridColumn: '1 / -1' }}>
                    {numberErrors.duplicate}
                  </span>
                )}
              </div>

              <button
                onClick={handleDrawNumbers}
                disabled={!selectedGameId || drawingNumbers !== null}
                className="draw-btn"
              >
                {drawingNumbers ? 'Drawing Numbers...' : 'Draw Numbers'}
              </button>
            </div>

            {loading ? (
              <p className="loading">Loading games...</p>
            ) : (
              <div className="games-list">
                <h3>Game Status</h3>
                {games.map((game) => (
                  <div key={game.id} className="game-item">
                    <div className="game-info">
                      <p><strong>{new Date(game.weekStart).toLocaleDateString()}</strong></p>
                      <p>Status: {game.isClosed ? '✅ Closed' : '⏳ Active'}</p>
                      {game.winningNumbers && (
                        <p>Winning: {game.winningNumbers.number1}, {game.winningNumbers.number2}, {game.winningNumbers.number3}</p>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default AdminPanel;
