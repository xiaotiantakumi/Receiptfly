import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { Layout } from './components/Layout/Layout';
import { Dashboard } from './pages/Dashboard/Dashboard';
import { Scan } from './pages/Scan/Scan';
import { ReceiptDetail } from './pages/ReceiptDetail/ReceiptDetail';

import { ReceiptProvider } from './context/ReceiptContext';

function App() {
  return (
    <ReceiptProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Layout />}>
            <Route index element={<Dashboard />} />
            <Route path="receipts/:id" element={<ReceiptDetail />} />
            <Route path="scan" element={<Scan />} />
            <Route path="settings" element={<div style={{padding: '2rem'}}>Settings (Coming Soon)</div>} />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </ReceiptProvider>
  );
}

export default App;
