import { useState, useEffect } from 'react'
import './App.css'

function App() {
  const [scrolled, setScrolled] = useState(false)

  useEffect(() => {
    const handleScroll = () => {
      setScrolled(window.scrollY > 50)
    }
    window.addEventListener('scroll', handleScroll)
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  return (
    <div className="app">
      {/* Navigation */}
      <nav className={`navbar ${scrolled ? 'scrolled' : ''}`}>
        <div className="nav-container">
          <div className="nav-logo">
            <h1>TaskFlow</h1>
          </div>
          <div className="nav-links">
            <a href="#features">Features</a>
            <a href="#how-it-works">How It Works</a>
            <a href="#tech-stack">Tech Stack</a>
            <a href="#cta" className="nav-cta">Get Started</a>
          </div>
        </div>
      </nav>

      {/* Hero Section */}
      <section className="hero">
        <div className="hero-content">
          <h1 className="hero-title">
            <span className="gradient-text">Modern E-Commerce</span>
            <br />
            Made Simple
          </h1>
          <p className="hero-subtitle">
            A powerful, scalable e-commerce solution built with .NET 10, React, and PostgreSQL.
            Deploy anywhere with Docker and secure your users with Google OAuth.
          </p>
          <div className="hero-buttons">
            <a href="#cta" className="btn btn-primary">
              Get Started Free
            </a>
            <a href="#features" className="btn btn-secondary">
              Learn More
            </a>
          </div>
          <div className="hero-stats">
            <div className="stat">
              <div className="stat-number">.NET 10</div>
              <div className="stat-label">Latest Framework</div>
            </div>
            <div className="stat">
              <div className="stat-number">Docker</div>
              <div className="stat-label">Container Ready</div>
            </div>
            <div className="stat">
              <div className="stat-number">OAuth</div>
              <div className="stat-label">Secure Auth</div>
            </div>
          </div>
        </div>
        <div className="hero-visual">
          <div className="floating-card card-1">
            <div className="card-icon">🛒</div>
            <div className="card-text">Shopping Cart</div>
          </div>
          <div className="floating-card card-2">
            <div className="card-icon">🔐</div>
            <div className="card-text">Secure Login</div>
          </div>
          <div className="floating-card card-3">
            <div className="card-icon">📦</div>
            <div className="card-text">Order Management</div>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section id="features" className="features">
        <div className="container">
          <h2 className="section-title">Powerful Features</h2>
          <p className="section-subtitle">Everything you need to build a modern e-commerce platform</p>
          <div className="features-grid">
            <div className="feature-card">
              <div className="feature-icon">⚡</div>
              <h3>Fast Performance</h3>
              <p>Built on .NET 10 with optimized React components for lightning-fast load times.</p>
            </div>
            <div className="feature-card">
              <div className="feature-icon">🔒</div>
              <h3>Secure Authentication</h3>
              <p>Google OAuth integration with secure cookie-based sessions.</p>
            </div>
            <div className="feature-card">
              <div className="feature-icon">🗄️</div>
              <h3>PostgreSQL Database</h3>
              <p>Robust data persistence with Entity Framework Core migrations.</p>
            </div>
            <div className="feature-card">
              <div className="feature-icon">🐳</div>
              <h3>Docker Ready</h3>
              <p>Containerized deployment with Docker Compose for easy scaling.</p>
            </div>
            <div className="feature-card">
              <div className="feature-icon">🔄</div>
              <h3>CI/CD Pipeline</h3>
              <p>Automated deployments with GitHub Actions to your VPS.</p>
            </div>
            <div className="feature-card">
              <div className="feature-icon">🌐</div>
              <h3>HTTPS Ready</h3>
              <p>Nginx Proxy Manager with Let's Encrypt for SSL certificates.</p>
            </div>
          </div>
        </div>
      </section>

      {/* How It Works Section */}
      <section id="how-it-works" className="how-it-works">
        <div className="container">
          <h2 className="section-title">How It Works</h2>
          <p className="section-subtitle">Get started in three simple steps</p>
          <div className="steps">
            <div className="step">
              <div className="step-number">1</div>
              <h3>Clone & Configure</h3>
              <p>Clone the repository and configure your environment variables.</p>
            </div>
            <div className="step">
              <div className="step-number">2</div>
              <h3>Build & Deploy</h3>
              <p>Run Docker Compose or use GitHub Actions for automated deployment.</p>
            </div>
            <div className="step">
              <div className="step-number">3</div>
              <h3>Go Live</h3>
              <p>Your e-commerce platform is ready with SSL and secure authentication.</p>
            </div>
          </div>
        </div>
      </section>

      {/* Tech Stack Section */}
      <section id="tech-stack" className="tech-stack">
        <div className="container">
          <h2 className="section-title">Built with Modern Technology</h2>
          <p className="section-subtitle">Production-ready stack with enterprise-grade tools</p>
          <div className="tech-grid">
            <div className="tech-item">
              <div className="tech-icon">🎯</div>
              <h3>.NET 10</h3>
              <p>Latest .NET framework for high-performance backend APIs</p>
            </div>
            <div className="tech-item">
              <div className="tech-icon">⚛️</div>
              <h3>React 19</h3>
              <p>Modern React with TypeScript for type-safe frontend</p>
            </div>
            <div className="tech-item">
              <div className="tech-icon">🐘</div>
              <h3>PostgreSQL 17</h3>
              <p>Robust relational database with advanced features</p>
            </div>
            <div className="tech-item">
              <div className="tech-icon">🐳</div>
              <h3>Docker</h3>
              <p>Container orchestration with Docker Compose</p>
            </div>
            <div className="tech-item">
              <div className="tech-icon">🔄</div>
              <h3>GitHub Actions</h3>
              <p>CI/CD pipeline for automated deployments</p>
            </div>
            <div className="tech-item">
              <div className="tech-icon">🔐</div>
              <h3>Google OAuth</h3>
              <p>Secure authentication with Google integration</p>
            </div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section id="cta" className="cta">
        <div className="container">
          <h2 className="cta-title">Ready to Build Your E-Commerce Platform?</h2>
          <p className="cta-subtitle">
            Get started with TaskFlow today and deploy your store in minutes.
          </p>
          <div className="cta-buttons">
            <a href="https://github.com/souravdas090300/TaskFlow" target="_blank" className="btn btn-primary btn-large">
              View on GitHub
            </a>
            <a href="#how-it-works" className="btn btn-secondary btn-large">
              Get Started
            </a>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="footer">
        <div className="container">
          <div className="footer-content">
            <div className="footer-brand">
              <h3>TaskFlow</h3>
              <p>Modern e-commerce made simple</p>
            </div>
            <div className="footer-links">
              <h4>Links</h4>
              <a href="#features">Features</a>
              <a href="#how-it-works">How It Works</a>
              <a href="#tech-stack">Tech Stack</a>
            </div>
            <div className="footer-links">
              <h4>Resources</h4>
              <a href="https://github.com/souravdas090300/TaskFlow" target="_blank">GitHub</a>
              <a href="https://docs.microsoft.com/dotnet/" target="_blank">.NET Docs</a>
              <a href="https://react.dev/" target="_blank">React Docs</a>
            </div>
          </div>
          <div className="footer-bottom">
            <p>&copy; 2026 TaskFlow. Built with ❤️ using .NET 10 and React.</p>
          </div>
        </div>
      </footer>
    </div>
  )
}

export default App
