# MyTrader API Migration Strategy & Backward Compatibility Matrix

**Document Version**: 1.1.0
**Effective Date**: 2024-09-24
**Contract Authority**: API Contract Governor
**Priority Implementation**: Based on RICE Scoring Analysis

## Executive Summary

This document outlines the comprehensive migration strategy for standardizing MyTrader's API and WebSocket contracts. The migration addresses critical connectivity issues, event naming mismatches, and API versioning inconsistencies identified through Business Analyst findings and Product Owner prioritization.

## Critical Issues & RICE Priority Analysis

| Issue | Description | RICE Score | Phase | Timeline |
|-------|-------------|------------|-------|----------|
| **WebSocket Port Fix** | Web connects to 8080, backend runs on 5002 | 576 | Immediate | 1 week |
| **Event Naming Standardization** | Backend/mobile event name mismatches | 432 | Phase 1 | 2 weeks |
| **API Versioning Consistency** | Mixed /api/ and /api/v1/ patterns | 288 | Phase 2 | 4 weeks |
| **Volume Leaders Endpoint** | Missing top volume assets endpoint | 216 | Phase 2 | 2 weeks |

## Migration Phases

### Phase 1: Critical WebSocket Fixes (Week 1)
**Priority**: RICE Score 576 - Production Breaking Issues

#### Objectives
- Fix port configuration mismatches
- Standardize WebSocket event naming
- Implement backward compatibility layer

#### Tasks
1. **Port Configuration Fix**
   - Update all environments to use port 5002 consistently
   - Update frontend configuration files
   - Deploy infrastructure changes
   - Validate cross-environment connectivity

2. **WebSocket Event Standardization**
   - Remove "Receive" prefixes from event names
   - Implement dual emission for backward compatibility
   - Update SignalR hub implementations
   - Deploy event name mapping layer

3. **Hub Consolidation**
   - Consolidate functionality into `/hubs/market-data`
   - Maintain legacy `/hubs/trading` with deprecation notices
   - Update client connection logic

#### Success Criteria
- [ ] All environments use port 5002 for WebSocket connections
- [ ] Mobile app receives standardized event names
- [ ] Web app connects successfully without 8080 dependency
- [ ] Legacy clients continue to function during transition

---

### Phase 2: API Versioning & Feature Completion (Weeks 2-4)
**Priority**: RICE Score 432-288 - High Impact Improvements

#### Objectives
- Unify API versioning to `/api/v1/` pattern
- Implement volume leaders endpoint
- Complete event naming migration
- Establish deprecation timeline for legacy endpoints

#### Tasks
1. **API Versioning Unification** (Weeks 2-3)
   - Migrate all controllers to `/api/v1/` pattern
   - Implement legacy route compatibility layer
   - Update client SDKs and configuration
   - Deploy versioning middleware

2. **Volume Leaders Endpoint** (Week 2)
   - Implement `GET /api/v1/market-data/top-by-volume`
   - Add database queries for volume ranking
   - Integrate with dashboard widgets
   - Performance optimize for sub-100ms response

3. **Complete Event Naming Migration** (Week 3)
   - Update frontend clients to use new event names
   - Remove dual emission after client validation
   - Update mobile app WebSocket service
   - Validate end-to-end connectivity

4. **Documentation & Client Updates** (Week 4)
   - Update API documentation
   - Release new client SDKs
   - Notify integration partners
   - Prepare deprecation announcements

#### Success Criteria
- [ ] All API endpoints follow `/api/v1/` pattern
- [ ] Volume leaders endpoint delivers <100ms response times
- [ ] Frontend clients use standardized event names
- [ ] Legacy routes function with deprecation warnings
- [ ] Dashboard displays volume leaders correctly

---

### Phase 3: Legacy Cleanup & Final Migration (Weeks 5-16)
**Priority**: Medium - Cleanup and Optimization

#### Objectives
- Remove backward compatibility layers
- Deprecate legacy endpoints
- Complete client migration
- Optimize performance

#### Tasks
1. **Legacy Route Deprecation** (Weeks 5-8)
   - Add deprecation headers to legacy endpoints
   - Monitor usage metrics for legacy routes
   - Send deprecation notices to clients
   - Plan removal timeline

2. **Client Migration Completion** (Weeks 6-12)
   - Work with frontend teams to complete migration
   - Update mobile app to remove legacy event handlers
   - Validate third-party integrations
   - Provide migration support

3. **Legacy Cleanup** (Weeks 13-16)
   - Remove dual emission layer
   - Delete legacy route handlers
   - Clean up deprecated hub endpoints
   - Archive migration code

#### Success Criteria
- [ ] Legacy routes removed after 90-day deprecation period
- [ ] All clients use standardized API contracts
- [ ] Performance metrics show improvement
- [ ] Monitoring shows zero legacy endpoint usage

## Backward Compatibility Matrix

### WebSocket Event Compatibility

| Legacy Event Name | New Standardized Name | Compatibility Period | Status |
|-------------------|----------------------|---------------------|---------|
| `ReceivePriceUpdate` | `PriceUpdate` | 90 days | Dual emission active |
| `ReceiveBatchPriceUpdate` | `BatchPriceUpdate` | 90 days | Dual emission active |
| `ReceiveMarketData` | `BatchPriceUpdate` | 90 days | Mapped to new event |
| `ReceiveMarketStatusUpdate` | `MarketStatusUpdate` | 90 days | Dual emission active |
| `ReceiveSignalUpdate` | `SignalUpdate` | 90 days | To be deprecated |
| `ReceiveSubscriptionConfirmed` | `SubscriptionConfirmed` | 90 days | Dual emission active |
| `ReceiveSubscriptionError` | `SubscriptionError` | 90 days | Dual emission active |

### API Endpoint Compatibility

| Legacy Endpoint | New Unified Endpoint | HTTP Method | Compatibility |
|----------------|---------------------|-------------|---------------|
| `/api/prices/live` | `/api/v1/market-data/batch` | GET | 90 days |
| `/api/symbols` | `/api/v1/symbols` | GET | 90 days |
| `/api/market-data/overview` | `/api/v1/market-data/overview` | GET | 90 days |
| `/api/auth/login` | `/api/v1/auth/login` | POST | 90 days |
| `/api/[controller]/*` | `/api/v1/{resource}/*` | ALL | 90 days |

### Hub Endpoint Compatibility

| Legacy Hub | New Unified Hub | Authentication | Status |
|------------|-----------------|----------------|---------|
| `/hubs/trading` | `/hubs/market-data` | Optional | Redirected with deprecation warning |
| `/hubs/dashboard` | `/hubs/market-data` | Optional | Functionality merged |
| `/hubs/mock-trading` | `/hubs/market-data` | Optional | Test environment only |

## Implementation Guidelines for Development Teams

### Frontend Web Team

1. **Configuration Updates**
   ```typescript
   // OLD - Remove after Phase 1
   const WS_URL = 'ws://localhost:8080/hubs/trading';

   // NEW - Use immediately
   const WS_URL = 'ws://localhost:5002/hubs/market-data';
   ```

2. **Event Handler Updates**
   ```typescript
   // OLD - Remove after Phase 2
   connection.on('ReceivePriceUpdate', handlePriceUpdate);

   // NEW - Implement immediately
   connection.on('PriceUpdate', handlePriceUpdate);
   ```

3. **API Endpoint Updates**
   ```typescript
   // OLD - Add deprecation warnings
   const response = await fetch('/api/prices/live');

   // NEW - Use after Phase 2
   const response = await fetch('/api/v1/market-data/batch', {
     method: 'POST',
     body: JSON.stringify({ symbolIds: [...] })
   });
   ```

### Frontend Mobile Team

1. **WebSocket Service Updates**
   ```typescript
   // Update websocketService.ts
   const hubUrl = API_BASE_URL.replace('/api', '/hubs/market-data');

   // Update event handlers
   this.connection.on('PriceUpdate', (data) => {
     // Handle standardized event
   });
   ```

2. **Configuration Updates**
   ```typescript
   // Ensure API_BASE_URL uses port 5002
   const API_BASE_URL = 'http://localhost:5002/api/v1';
   const WS_BASE_URL = 'ws://localhost:5002/hubs/market-data';
   ```

### Backend Team

1. **Controller Migration Pattern**
   ```csharp
   // NEW - Unified versioning pattern
   [ApiController]
   [Route("api/v1/market-data")]
   public class MarketDataController : ControllerBase
   {
       // Implementation
   }

   // LEGACY - Add deprecation attributes
   [ApiController]
   [Route("api/market-data")]
   [Obsolete("Use /api/v1/market-data instead. This endpoint will be removed on 2024-12-24.")]
   public class LegacyMarketDataController : ControllerBase
   {
       // Redirect to new endpoint
   }
   ```

2. **WebSocket Hub Updates**
   ```csharp
   // NEW - Standardized event names
   await Clients.All.SendAsync("PriceUpdate", marketData);

   // DUAL EMISSION - Temporary backward compatibility
   await Clients.All.SendAsync("ReceivePriceUpdate", marketData);
   ```

3. **Volume Leaders Implementation**
   ```csharp
   [HttpGet("top-by-volume")]
   public async Task<ActionResult<ApiResponse<VolumeLeadersResponse>>> GetTopByVolume(
       [FromQuery] int perClass = 8,
       [FromQuery] string? assetClasses = null,
       [FromQuery] string timeframe = "24h")
   {
       // Implementation with <100ms target response time
   }
   ```

## Validation Rules and Testing Strategies

### Pre-Migration Validation

1. **Connectivity Tests**
   - Verify port 5002 accessibility across all environments
   - Test WebSocket connection establishment
   - Validate SSL/TLS configuration for production

2. **Event Flow Validation**
   - Test dual emission of old and new event names
   - Verify payload compatibility between event versions
   - Validate client-side event handler mapping

3. **API Contract Validation**
   - Verify OpenAPI specification compliance
   - Test request/response schema validation
   - Validate error response consistency

### Migration Testing Strategy

1. **Phase 1 Testing** (Week 1)
   ```bash
   # Port connectivity test
   curl -I http://localhost:5002/health

   # WebSocket connection test
   wscat -c ws://localhost:5002/hubs/market-data

   # Event dual emission verification
   # Monitor both old and new event names in client logs
   ```

2. **Phase 2 Testing** (Weeks 2-4)
   ```bash
   # API versioning test
   curl http://localhost:5002/api/v1/market-data/overview
   curl http://localhost:5002/api/market-data/overview  # Should show deprecation

   # Volume leaders endpoint test
   curl "http://localhost:5002/api/v1/market-data/top-by-volume?perClass=8"

   # Response time validation
   time curl http://localhost:5002/api/v1/market-data/top-by-volume
   ```

3. **Phase 3 Testing** (Weeks 5-16)
   ```bash
   # Legacy endpoint removal verification
   curl http://localhost:5002/api/prices/live  # Should return 404

   # Client migration validation
   # Monitor logs for legacy event handler usage
   ```

### Automated Testing Requirements

1. **Contract Tests**
   - API contract compliance validation
   - WebSocket event schema validation
   - Backward compatibility verification

2. **Performance Tests**
   - Response time validation (<100ms for volume leaders)
   - WebSocket message throughput testing
   - Concurrent connection testing

3. **Integration Tests**
   - End-to-end client connectivity
   - Cross-asset class functionality
   - Error scenario handling

## Risk Management

### High-Risk Areas

1. **Port Configuration Changes**
   - **Risk**: Client connection failures
   - **Mitigation**: Gradual rollout with monitoring
   - **Rollback**: Immediate DNS/load balancer revert

2. **Event Name Changes**
   - **Risk**: Mobile app crashes on unknown events
   - **Mitigation**: Dual emission during transition
   - **Rollback**: Restore old event names only

3. **API Versioning Changes**
   - **Risk**: Breaking third-party integrations
   - **Mitigation**: 90-day deprecation period with warnings
   - **Rollback**: Maintain legacy endpoints longer

### Monitoring and Alerting

1. **Connection Metrics**
   - WebSocket connection success/failure rates
   - Client connection distribution by version
   - Error rates by endpoint version

2. **Performance Metrics**
   - API response times by endpoint
   - WebSocket message latency
   - Volume leaders endpoint performance

3. **Migration Progress Metrics**
   - Legacy endpoint usage over time
   - Client version adoption rates
   - Error rates during migration phases

## Success Criteria

### Phase 1 Success Metrics
- [ ] 100% of connections use port 5002
- [ ] 0% increase in WebSocket connection errors
- [ ] Mobile app receives both old and new event names
- [ ] Web app connects without 8080 dependency

### Phase 2 Success Metrics
- [ ] 90% of API calls use `/api/v1/` endpoints
- [ ] Volume leaders endpoint <100ms 95th percentile
- [ ] Frontend clients use standardized event names
- [ ] Legacy endpoints show deprecation warnings

### Phase 3 Success Metrics
- [ ] 0% usage of legacy endpoints
- [ ] 100% client migration completion
- [ ] Performance improvement measurements
- [ ] Clean codebase without migration artifacts

## Communication Plan

### Stakeholder Notifications

1. **Week -1**: Pre-migration announcement to all teams
2. **Week 0**: Phase 1 deployment notification
3. **Week 2**: Phase 2 deployment notification
4. **Week 12**: 90-day deprecation warnings begin
5. **Week 16**: Legacy cleanup completion notice

### Documentation Updates

1. **API Documentation**: Update OpenAPI specs and examples
2. **Client SDKs**: Release updated versions with new contracts
3. **Integration Guides**: Update third-party integration documentation
4. **Troubleshooting**: Add migration-specific troubleshooting guides

## Conclusion

This migration strategy provides a comprehensive, risk-managed approach to standardizing MyTrader's API and WebSocket contracts. The phased approach ensures minimal disruption to production systems while systematically addressing the identified critical issues.

The success of this migration depends on:
1. Adherence to the defined timeline
2. Thorough testing at each phase
3. Proactive communication with all stakeholders
4. Continuous monitoring and rapid issue resolution

By following this strategy, MyTrader will achieve a unified, consistent API contract that supports future scalability and reduces integration complexity for all clients.

---

**Document Approval**:
- **API Contract Governor**: [Signature Required]
- **Backend Team Lead**: [Signature Required]
- **Frontend Team Lead**: [Signature Required]
- **Product Owner**: [Signature Required]

**Next Review Date**: 2024-10-24