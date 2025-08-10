import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

export const roleGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  
  const requiredRoles = route.data['roles'] as string[];
  
  if (!requiredRoles || requiredRoles.length === 0) {
    return true;
  }
  
  return authService.currentUser$.pipe(
    take(1),
    map(user => {
      if (user && requiredRoles.some(role => user.roles.includes(role))) {
        return true;
      }
      
      router.navigate(['/unauthorized']);
      return false;
    })
  );
};