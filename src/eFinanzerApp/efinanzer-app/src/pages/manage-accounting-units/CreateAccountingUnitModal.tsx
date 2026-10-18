import { useState } from 'react';
import { App, Form, Input, Modal } from 'antd';
import apiClient from '../../api/client';
import {
  DESCRIPTION_MAX_LENGTH,
  KEY_MAX_LENGTH,
  NAME_MAX_LENGTH,
  descriptionRules,
  emptyToNull,
  keyRules,
  nameRules,
} from './accountingUnitValidation';

interface CreateAccountingUnitFormValues {
  key: string;
  name?: string;
  description?: string;
}

interface CreateAccountingUnitModalProps {
  open: boolean;
  onClose: () => void;
  /** Called after the accounting unit was created */
  onCreated: () => void;
}

function CreateAccountingUnitModal({ open, onClose, onCreated }: CreateAccountingUnitModalProps) {
  const { message } = App.useApp();
  const [form] = Form.useForm<CreateAccountingUnitFormValues>();
  const [saving, setSaving] = useState(false);

  const handleSubmit = async (values: CreateAccountingUnitFormValues) => {
    setSaving(true);
    try {
      const { response } = await apiClient.POST('/api/manage-accounting-units/create-accounting-unit', {
        body: { key: values.key, name: emptyToNull(values.name), description: emptyToNull(values.description) },
      });

      if (response.ok) {
        message.success('Haushalt wurde angelegt');
        onCreated();
        onClose();
      } else if (response.status === 409) {
        form.setFields([{ name: 'key', errors: ['Dieser Schlüssel ist bereits vergeben'] }]);
      } else if (response.status === 400) {
        message.error('Die Eingaben sind ungültig');
      } else {
        message.error(`Haushalt konnte nicht angelegt werden (HTTP ${response.status})`);
      }
    } catch {
      message.error('Haushalt konnte nicht angelegt werden');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      title="Haushalt anlegen"
      open={open}
      onCancel={onClose}
      onOk={() => form.submit()}
      okText="Anlegen"
      confirmLoading={saving}
      afterClose={() => form.resetFields()}
      destroyOnHidden
    >
      <Form form={form} layout="vertical" onFinish={handleSubmit} requiredMark="optional">
        <Form.Item
          name="key"
          label="Schlüssel"
          extra="Eindeutige Kennung, kann später nicht geändert werden"
          rules={keyRules}
        >
          <Input maxLength={KEY_MAX_LENGTH} autoFocus />
        </Form.Item>
        <Form.Item name="name" label="Name" rules={nameRules}>
          <Input maxLength={NAME_MAX_LENGTH} />
        </Form.Item>
        <Form.Item name="description" label="Beschreibung" rules={descriptionRules}>
          <Input.TextArea maxLength={DESCRIPTION_MAX_LENGTH} showCount autoSize={{ minRows: 2, maxRows: 6 }} />
        </Form.Item>
      </Form>
    </Modal>
  );
}

export default CreateAccountingUnitModal;
