import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { transactionService, gameService } from '../services/api';
import { PendingTransaction, Game } from '../types';
import './AdminPanel.css';

const AdminPanel: React.FC = () => {
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<'transactions' | 'games'>('transactions');
  const [pendingTransactions, setPendingTransactions] = useState<PendingTransaction[]>([]);
  const [games, setGames] = useState<Game[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [approving, setApproving] = useState<string | null>(null);
  const [drawingNumbers, setDrawingNumbers] = useState<string | null>(null);

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

      const [transRes, gamesRes] = await Promise.all([
        transactionService.getPendingTransactions(),
        gameService.getAllGames(),
      ]);

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

  const handleApproveTransaction = async (transactionId: string) => {
    try {
      setApproving(transactionId);
      await transactionService.approveTransaction(transactionId);

      // Remove from list
      setPendingTransactions((prev) =>
        prev.filter((t) => t.id !== transactionId)
      );

      setApproving(null);
    } catch (err: any) {
      console.error('Error approving transaction:', err);
      setError('Failed to approve transaction. Please try again.');
      setApproving(null);
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

  return (
    <div className="admin-container">
      <nav className="admin-navbar">
        <h1 className="admin-logo">Dead Pigeons - Admin Panel</h1>
        <button onClick={() => navigate('/dashboard')} className="back-btn">
          Back to Dashboard
        </button>
      </nav>

      <div className="admin-content">
        <div className="admin-tabs">
          <button
            className={`admin-tab ${activeTab === 'transactions' ? 'active' : ''}`}
            onClick={() => setActiveTab('transactions')}
          >
            Pending Transactions
          </button>
          <button
            className={`admin-tab ${activeTab === 'games' ? 'active' : ''}`}
            onClick={() => setActiveTab('games')}
          >
            Draw Numbers
          </button>
        </div>

        {error && <div className="error-message">{error}</div>}

        {activeTab === 'transactions' && (
          <div className="admin-section">
            <h2>Pending Transactions</h2>
            {loading ? (
              <p className="loading">Loading transactions...</p>
            ) : pendingTransactions.length === 0 ? (
              <p className="empty-state">No pending transactions.</p>
            ) : (
              <div className="transactions-table">
                <table>
                  <thead>
                    <tr>
                      <th>Player Name</th>
                      <th>Email</th>
                      <th>Amount</th>
                      <th>MobilePay ID</th>
                      <th>Date</th>
                      <th>Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {pendingTransactions.map((trans) => (
                      <tr key={trans.id}>
                        <td>{trans.playerName || 'N/A'}</td>
                        <td>{trans.playerEmail}</td>
                        <td className="amount">{trans.amount} DKK</td>
                        <td className="mobile-id">{trans.mobilePayId}</td>
                        <td>{new Date(trans.createdAt).toLocaleDateString()}</td>
                        <td>
                          <button
                            onClick={() => handleApproveTransaction(trans.id)}
                            disabled={approving === trans.id}
                            className="approve-btn"
                          >
                            {approving === trans.id ? 'Approving...' : 'Approve'}
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
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
  );
};

export default AdminPanel;
