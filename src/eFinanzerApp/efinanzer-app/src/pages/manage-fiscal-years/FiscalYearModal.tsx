import { useState } from 'react';
import { App, DatePicker, Form, Input, Modal } from 'antd';
import dayjs from 'dayjs';
import apiClient from '../../common/api/client';
import type { FiscalYearListItem } from '../../common/api/types';
import type { components } from '../../common/api/schema';
import { emptyToNull } from '../manage-accounting-units/accountingUnitValidation';
import {
  KEY_MAX_LENGTH,
  MIN_DATE,
  NAME_MAX_LENGTH,
  formatDate,
  keyRules,
  nameRules,
  periodRules,
  toApiDate,
} from './fiscalYearValidation';
import type { Period } from './fiscalYearValidation';

type InvalidInputError = components['schemas']['InvalidInputError'];
type OverlapsExistingError = components['schemas']['FiscalYearOverlapsExistingError'];

interface FiscalYearFormValues {
  key: string;
  name?: string;
  period: Period;
}

// Field names of the API's InvalidInputError mapped to the form fields
const formFieldsByApiField: Record<string, keyof FiscalYearFormValues> = {
  Key: 'key',
  Name: 'name',
  StartDate: 'period',
  EndDate: 'period',
};

// Both conflicts of the create and the update endpoint share the status code, only the overlap names another year
function isOverlapError(error: unknown): error is OverlapsExistingError {
  return typeof error === 'object' && error !== null && 'overlappingKey' in error;
}

function isInvalidInputError(error: unknown): error is InvalidInputError {
  return typeof error === 'object' && error !== null && 'field' in error;
}

interface FiscalYearModalProps {
  open: boolean;
  accountingUnitKey: string;
  /** The fiscal year to edit, creates a new one if undefined */
  fiscalYear?: FiscalYearListItem;
  /** Prefilled period of a new fiscal year */
  suggestedPeriod?: Period;
  onClose: () => void;
  /** Called after the fiscal year was saved */
  onSaved: () => void;
}

// Remounted for every opening (see ManageFiscalYearsPage), so the initial values always match the shown fiscal year
function FiscalYearModal({ open, accountingUnitKey, fiscalYear, suggestedPeriod, onClose, onSaved }: FiscalYearModalProps) {
  const { message } = App.useApp();
  const [form] = Form.useForm<FiscalYearFormValues>();
  const [saving, setSaving] = useState(false);
  const isEdit = fiscalYear !== undefined;
  const hasReceipts = Number(fiscalYear?.receiptsCount ?? 0) > 0;

  const initialValues: Partial<FiscalYearFormValues> = fiscalYear
    ? {
        key: fiscalYear.key,
        name: fiscalYear.name ?? undefined,
        period: [dayjs(fiscalYear.startDate), dayjs(fiscalYear.endDate)],
      }
    : { period: suggestedPeriod };

  const send = (values: FiscalYearFormValues) => {
    const name = emptyToNull(values.name);
    const startDate = toApiDate(values.period[0]);
    const endDate = toApiDate(values.period[1]);

    if (isEdit) {
      return apiClient.PUT('/api/manage-fiscal-years/update-fiscal-year', {
        params: { query: { accountingUnitKey, key: fiscalYear.key } },
        body: { name, startDate, endDate },
      });
    }
    return apiClient.POST('/api/manage-fiscal-years/create-fiscal-year', {
      body: { key: values.key, accountingUnitKey, name, startDate, endDate },
    });
  };

  const handleSubmit = async (values: FiscalYearFormValues) => {
    const failureText = isEdit ? 'Haushaltsjahr konnte nicht gespeichert werden' : 'Haushaltsjahr konnte nicht angelegt werden';

    setSaving(true);
    try {
      const { response, error } = await send(values);

      if (response.ok) {
        message.success(isEdit ? 'Haushaltsjahr wurde gespeichert' : 'Haushaltsjahr wurde angelegt');
        onSaved();
        onClose();
      } else if (response.status === 409 && isOverlapError(error)) {
        const other = `„${error.overlappingKey}“ (${formatDate(error.overlappingStartDate)} – ${formatDate(error.overlappingEndDate)})`;
        form.setFields([{ name: 'period', errors: [`Überschneidet sich mit dem Haushaltsjahr ${other}`] }]);
      } else if (response.status === 409 && isEdit) {
        form.setFields([{ name: 'period', errors: ['Es gibt Belege außerhalb dieses Zeitraums'] }]);
      } else if (response.status === 409) {
        form.setFields([{ name: 'key', errors: ['Dieser Schlüssel ist bereits vergeben'] }]);
      } else if (response.status === 400 && isInvalidInputError(error) && formFieldsByApiField[error.field]) {
        form.setFields([{ name: formFieldsByApiField[error.field], errors: ['Ungültige Eingabe'] }]);
      } else if (response.status === 400) {
        message.error('Die Eingaben sind ungültig');
      } else if (response.status === 404) {
        message.error(isEdit ? 'Das Haushaltsjahr existiert nicht mehr' : 'Der Haushalt existiert nicht mehr');
        onSaved();
        onClose();
      } else {
        message.error(`${failureText} (HTTP ${response.status})`);
      }
    } catch {
      message.error(failureText);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      title={isEdit ? `Haushaltsjahr „${fiscalYear.name ?? fiscalYear.key}“ bearbeiten` : 'Haushaltsjahr anlegen'}
      open={open}
      onCancel={onClose}
      onOk={() => form.submit()}
      okText={isEdit ? 'Speichern' : 'Anlegen'}
      confirmLoading={saving}
      destroyOnHidden
    >
      <Form form={form} layout="vertical" onFinish={handleSubmit} requiredMark="optional" initialValues={initialValues}>
        <Form.Item
          name="key"
          label="Schlüssel"
          extra={isEdit ? undefined : 'Eindeutig innerhalb des Haushalts, kann später nicht geändert werden'}
          rules={keyRules}
        >
          <Input maxLength={KEY_MAX_LENGTH} disabled={isEdit} autoFocus={!isEdit} />
        </Form.Item>
        <Form.Item name="name" label="Name" rules={nameRules}>
          <Input maxLength={NAME_MAX_LENGTH} autoFocus={isEdit} />
        </Form.Item>
        <Form.Item
          name="period"
          label="Zeitraum"
          extra={hasReceipts ? 'Alle Belege des Haushaltsjahres müssen im Zeitraum liegen' : undefined}
          rules={periodRules}
        >
          <DatePicker.RangePicker
            format="DD.MM.YYYY"
            minDate={MIN_DATE}
            allowClear={false}
            style={{ width: '100%' }}
          />
        </Form.Item>
      </Form>
    </Modal>
  );
}

export default FiscalYearModal;
