import { Outlet, NavLink } from 'react-router-dom';
import { Home, Camera, Settings, PieChart } from 'lucide-react';
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
            <Home size={24} />
            <span className={styles.navLabel}>ホーム</span>
          </NavLink>
          
          <NavLink to="/analytics" className={({ isActive }) => `${styles.navItem} ${isActive ? styles.active : ''}`}>
            <PieChart size={24} />
            <span className={styles.navLabel}>集計</span>
          </NavLink>

          <NavLink 
            to="/scan" 
            className={({ isActive }) => `${styles.navItem} ${isActive ? styles.active : ''}`}
          >
            <Camera size={24} />
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
