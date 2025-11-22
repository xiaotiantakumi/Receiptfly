import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Layout } from './components/Layout/Layout';
import { Dashboard } from './pages/Dashboard/Dashboard';
import { Scan } from './pages/Scan/Scan';
import { ReceiptDetail } from './pages/ReceiptDetail/ReceiptDetail';
import Analytics from './pages/Analytics/Analytics';
import { ManualEntry } from './pages/ManualEntry/ManualEntry';
import { Settings } from './pages/Settings/Settings';

import { ReceiptProvider } from './context/ReceiptContext';
import { SettingsProvider } from './context/SettingsContext';

function App() {
  return (
    <SettingsProvider>
      <ReceiptProvider>
        <BrowserRouter>
          <Routes>
            <Route path="/" element={<Layout />}>
              <Route index element={<Dashboard />} />
              <Route path="scan" element={<Scan />} />
              <Route path="receipts/:id" element={<ReceiptDetail />} />
              <Route path="analytics" element={<Analytics />} />
              <Route path="manual-entry" element={<ManualEntry />} />
              <Route path="settings" element={<Settings />} />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
          </Routes>
        </BrowserRouter>
      </ReceiptProvider>
    </SettingsProvider>
  );
}

export default App;
