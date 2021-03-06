import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { LocalizationModule } from 'angular-l10n';

import { AuthGuardService } from '../../core/services/authGuard.service';
import { CanDeactivateGuardService } from '../../core/services/canDeactivateGuard.service';

import { SharedModule } from '../../shared/shared.module';

import { ContactComponent } from './components/contact/contact.component';

@NgModule({
    declarations: [
        ContactComponent
    ],
    imports: [
        // Angular Modules
        RouterModule.forChild([
            {
                path: 'contact',
                component: ContactComponent,
                data: { auth: true },
                canActivate: [AuthGuardService],
                canDeactivate: [CanDeactivateGuardService],
                canActivateChild: [AuthGuardService],
                children: []
            }
        ]),
        // My Modules
        SharedModule
    ],
    exports: [ContactComponent],
    providers: [
    ]
})
export class ContactModule {
}