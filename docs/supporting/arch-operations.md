# Operations & Maintenance

## Backup Strategy

### What to Backup
- `./data` directory - Contains all application data
- `.env` file - Environment configuration
- `config/` directory - Application configuration
- `projects/` directory - Project files
- Database files (SQLite or PostgreSQL dumps)

### Backup Schedule
- Daily automated backups
- Retain last 7 daily backups
- Retain last 4 weekly backups
- Retain last 3 monthly backups

### Backup Process
1. Stop write operations (optional for consistency)
2. Copy data directory to backup location
3. Export database if using PostgreSQL
4. Compress backup files
5. Verify backup integrity
6. Resume normal operations

## Recovery Process

### Full Recovery
1. Stop all services
2. Restore data directory from backup
3. Restore configuration files
4. Restore database if needed
5. Start services
6. Verify functionality

### Partial Recovery
- Individual project recovery
- Configuration-only recovery
- Provider settings recovery
- User preferences recovery

## Database Management

### PostgreSQL (Primary)
- Primary database for all deployments
- pg_dump for backups
- Point-in-time recovery support
- Replication for high availability
- Connection pooling via PgBouncer

### SQLite (Development Only)
- Local development testing
- Automatic daily snapshots
- WAL mode for better concurrency
- VACUUM weekly for optimization
- Backup via file copy

## Log Management

### Log Locations
- Application logs: `./data/logs/app.log`
- Error logs: `./data/logs/error.log`
- Access logs: `./data/logs/access.log`
- Audit logs: `./data/logs/audit.log`

### Log Rotation
- Daily rotation
- Compress after rotation
- Retain 30 days of logs
- Archive older logs to cold storage

### Log Levels
- `ERROR`: System failures requiring attention
- `WARN`: Performance issues, deprecations
- `INFO`: User actions, system events
- `DEBUG`: Detailed debugging information

## Performance Monitoring

### Key Metrics
- Response time (p50, p95, p99)
- Throughput (requests/second)
- Error rate
- Token usage per provider
- Active connections
- Memory usage
- CPU usage

### Monitoring Tools
- Built-in `/metrics` endpoint
- Prometheus export (optional)
- Health check endpoints
- Custom dashboard

## Maintenance Tasks

### Daily
- Check health endpoints
- Review error logs
- Monitor disk space
- Verify backups

### Weekly
- Database optimization (VACUUM for SQLite)
- Log rotation verification
- Security updates check
- Performance review

### Monthly
- Full backup verification
- Capacity planning review
- Cost analysis
- Security audit

## Upgrade Process

### Rolling Upgrade (Zero Downtime)
1. Deploy new version to staging
2. Run smoke tests
3. Deploy to production (blue-green)
4. Monitor for issues
5. Switch traffic to new version
6. Keep old version for rollback

### Standard Upgrade
1. Announce maintenance window
2. Backup current state
3. Stop services
4. Deploy new version
5. Run migrations if needed
6. Start services
7. Verify functionality

## Troubleshooting

### Common Issues

| Issue | Diagnosis | Solution |
|-------|-----------|----------|
| High memory usage | Check provider connections | Restart providers, check for leaks |
| Slow responses | Review token usage | Optimize context, check rate limits |
| Connection failures | Check network/firewall | Verify endpoints, check certificates |
| Database locks | Long-running transactions | Optimize queries, increase timeout |

### Debug Mode
Enable debug logging:
- Set `LOG_LEVEL=debug`
- Enable stack traces
- Capture all API calls
- Record performance metrics

## Security Maintenance

### Regular Tasks
- Rotate API keys quarterly
- Update TLS certificates before expiry
- Review access logs for anomalies
- Update dependencies monthly
- Security scanning weekly

### Incident Response
1. Isolate affected systems
2. Preserve logs for analysis
3. Patch vulnerabilities
4. Restore from clean backup if needed
5. Document incident
6. Review and improve procedures