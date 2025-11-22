import { Outlet, NavLink } from 'react-router-dom';
import { LayoutDashboard, ScanLine, Settings } from 'lucide-react';
import styles from './Layout.module.css';

export function Layout() {
  return (
    <div className={styles.container}>
      <main className={styles.main}>
        <Outlet />
      </main>
      
      <nav className={styles.nav}>
        <div className={styles.navContent}>
          <NavLink 
            to="/" 
            className={({ isActive }) => `${styles.navItem} ${isActive ? styles.active : ''}`}
          >
            <LayoutDashboard size={24} />
            <span className={styles.navLabel}>ダッシュボード</span>
          </NavLink>
          
          <NavLink 
            to="/scan" 
            className={({ isActive }) => `${styles.navItem} ${styles.scanButton} ${isActive ? styles.active : ''}`}
          >
            <div className={styles.scanIconWrapper}>
              <ScanLine size={28} />
            </div>
            <span className={styles.navLabel}>スキャン</span>
          </NavLink>

          <NavLink 
            to="/settings" 
            className={({ isActive }) => `${styles.navItem} ${isActive ? styles.active : ''}`}
          >
            <Settings size={24} />
            <span className={styles.navLabel}>設定</span>
          </NavLink>
        </div>
      </nav>
    </div>
  );
}
